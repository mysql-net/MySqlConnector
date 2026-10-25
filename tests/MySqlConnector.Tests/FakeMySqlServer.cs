using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace MySqlConnector.Tests;

public sealed class FakeMySqlServer
{
	public FakeMySqlServer()
	{
		m_tcpListener = new(IPAddress.Any, 0);
		m_lock = new();
		m_connections = [];
		m_tasks = [];
		m_clearPasswordResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	public void Start()
	{
		m_activeConnections = 0;
		m_cts = new();
		m_tcpListener.Start();
		m_tasks.Add(AcceptConnectionsAsync());
	}

	public void Reset()
	{
		m_cts.Cancel();
		try
		{
			Task.WaitAll(m_tasks.Skip(1).ToArray());
		}
		catch (AggregateException)
		{
		}
		m_connections.Clear();
		m_tasks.Clear();
		m_cts.Dispose();
		m_cts = new();
	}

	public void Stop()
	{
		if (m_cts is not null)
		{
			m_cts.Cancel();
			m_tcpListener.Stop();
			try
			{
				Task.WaitAll(m_tasks.ToArray());
			}
			catch (AggregateException)
			{
			}
			m_connections.Clear();
			m_tasks.Clear();
#if NET8_0_OR_GREATER
			m_tcpListener.Dispose();
#endif
			m_cts.Dispose();
			m_cts = null;
		}
	}

	public int Port => ((IPEndPoint) m_tcpListener.LocalEndpoint).Port;

	public int ActiveConnections => m_activeConnections;

	public string ServerVersion { get; set; } = "5.7.10-test";

	public bool SuppressAuthPluginNameTerminatingNull { get; set; }
	public bool SendIncompletePostHandshakeResponse { get; set; }
	public TimeSpan? ConnectDelay { get; set; }
	public TimeSpan? ResetDelay { get; set; }

	// When set, the server advertises TLS support in its initial handshake and performs the server side of a TLS
	// handshake (using this certificate) when the client requests it.
	public X509Certificate2 ServerCertificate { get; set; }

	// When true (and ServerCertificate is set), the server requests a switch to "mysql_clear_password" authentication
	// immediately after the TLS handshake, then records what the client sends in response (see ClearPasswordResponse).
	public bool RequestClearPasswordSwitch { get; set; }

	// Completes with the payload the client sends in response to a "mysql_clear_password" auth switch request, or with
	// null if the client refused to send its password and instead closed the connection.
	public Task<byte[]> ClearPasswordResponse => m_clearPasswordResponse.Task;

	internal void CancelQuery(int connectionId)
	{
		lock (m_lock)
		{
			if (connectionId >= 1 && connectionId <= m_connections.Count)
				m_connections[connectionId - 1].CancelQueryEvent.Set();
		}
	}

	internal void ClientDisconnected() => Interlocked.Decrement(ref m_activeConnections);

	internal void SetClearPasswordResponse(byte[] response) => m_clearPasswordResponse.TrySetResult(response);

	internal void SetServerException(Exception exception) => m_clearPasswordResponse.TrySetException(exception);

	private async Task AcceptConnectionsAsync()
	{
		while (true)
		{
			var tcpClient = await m_tcpListener.AcceptTcpClientAsync();
			Interlocked.Increment(ref m_activeConnections);
			lock (m_lock)
			{
				var connection = new FakeMySqlServerConnection(this, m_tasks.Count);
				m_connections.Add(connection);
				m_tasks.Add(connection.RunAsync(tcpClient, m_cts.Token));
			}
		}
	}

	private readonly object m_lock;
	private readonly TcpListener m_tcpListener;
	private readonly List<FakeMySqlServerConnection> m_connections;
	private readonly List<Task> m_tasks;
	private readonly TaskCompletionSource<byte[]> m_clearPasswordResponse;
	private CancellationTokenSource m_cts;
	private int m_activeConnections;
}
