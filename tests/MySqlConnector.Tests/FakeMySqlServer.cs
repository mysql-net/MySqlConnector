using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MySqlConnector.Tests
{
	public sealed class FakeMySqlServer
	{
		public FakeMySqlServer()
		{
			m_tcpListener = new TcpListener(IPAddress.Any, 0);
			m_tasks = new List<Task>();
		}

		public void Start()
		{
			m_activeConnections = 0;
			m_cts = new CancellationTokenSource();
			m_tcpListener.Start();
			m_tasks.Add(AcceptConnectionsAsync());
		}

		public void Stop()
		{
			if (m_cts != null)
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
				m_tasks.Clear();
				m_cts.Dispose();
				m_cts = null;
			}
		}

		public int Port => ((IPEndPoint) m_tcpListener.LocalEndpoint).Port;

		public int ActiveConnections => m_activeConnections;

		public string ServerVersion { get; set; } = "5.7.10-test";

		public bool SuppressAuthPluginNameTerminatingNull { get; set; }
		public bool SendIncompletePostHandshakeResponse { get; set; }

		internal void ClientDisconnected() => Interlocked.Decrement(ref m_activeConnections);

		private async Task AcceptConnectionsAsync()
		{
			while (true)
			{
				var tcpClient = await m_tcpListener.AcceptTcpClientAsync();
				Interlocked.Increment(ref m_activeConnections);
				lock (m_tasks)
				{
					var connection = new FakeMySqlServerConnection(this, m_tasks.Count);
					m_tasks.Add(connection.RunAsync(tcpClient, m_cts.Token));
				}
			}
		}

		readonly TcpListener m_tcpListener;
		readonly List<Task> m_tasks;
		CancellationTokenSource m_cts;
		int m_activeConnections;
	}
}
