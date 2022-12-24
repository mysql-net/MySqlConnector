#if NET7_0_OR_GREATER
using Microsoft.Extensions.Logging;
using MySqlConnector.Core;
using MySqlConnector.Logging;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector;

/// <summary>
/// <see cref="MySqlDataSource"/> implements a MySQL data source which can be used to obtain open connections, and against which commands can be executed directly.
/// </summary>
public sealed class MySqlDataSource : DbDataSource
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MySqlDataSource"/> class.
	/// </summary>
	/// <param name="connectionString">The connection string for the MySQL Server. This parameter is required.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="connectionString"/> is <c>null</c>.</exception>
	public MySqlDataSource(string connectionString)
		: this(connectionString ?? throw new ArgumentNullException(nameof(connectionString)), MySqlConnectorLoggingConfiguration.NullConfiguration)
	{
	}

	internal MySqlDataSource(string connectionString, MySqlConnectorLoggingConfiguration loggingConfiguration)
	{
		m_connectionString = connectionString;
		LoggingConfiguration = loggingConfiguration;
		m_logger = loggingConfiguration.DataSourceLogger;

		Pool = ConnectionPool.CreatePool(m_connectionString);
		m_id = Interlocked.Increment(ref s_lastId);
		if (Pool is not null)
			LogMessages.DataSourceCreatedWithPool(m_logger, m_id, Pool.Id);
		else
			LogMessages.DataSourceCreatedWithoutPool(m_logger, m_id);
	}

	/// <summary>
	/// Creates a new <see cref="MySqlConnection"/> that can connect to the database represented by this <see cref="MySqlDataSource"/>.
	/// </summary>
	/// <remarks>
	/// <para>The connection must be opened before it can be used.</para>
	/// <para>It is the responsibility of the caller to properly dispose the connection returned by this method. Failure to do so may result in a connection leak.</para>
	/// </remarks>
	public new MySqlConnection CreateConnection() => (MySqlConnection) base.CreateConnection();

	/// <summary>
	/// Returns a new, open <see cref="MySqlConnection"/> to the database represented by this <see cref="MySqlDataSource"/>.
	/// </summary>
	/// <remarks>
	/// <para>The returned connection is already open, and is ready for immediate use.</para>
	/// <para>It is the responsibility of the caller to properly dispose the connection returned by this method. Failure to do so may result in a connection leak.</para>
	/// </remarks>
	public new MySqlConnection OpenConnection() => (MySqlConnection) base.OpenConnection();

	/// <summary>
	/// Asynchronously returns a new, open <see cref="MySqlConnection"/> to the database represented by this <see cref="MySqlDataSource"/>.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <remarks>
	/// <para>The returned connection is already open, and is ready for immediate use.</para>
	/// <para>It is the responsibility of the caller to properly dispose the connection returned by this method. Failure to do so may result in a connection leak.</para>
	/// </remarks>
	public new async ValueTask<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) =>
		(MySqlConnection) await base.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Gets the connection string of the database represented by this <see cref="MySqlDataSource"/>.
	/// </summary>
	public override string ConnectionString => m_connectionString;

	protected override DbConnection CreateDbConnection()
	{
		if (m_isDisposed)
			throw new ObjectDisposedException(nameof(MySqlDataSource));
		return new MySqlConnection(this);
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
#pragma warning disable CA2012 // Safe because method completes synchronously
			if (disposing)
				DisposeAsync(IOBehavior.Synchronous).GetAwaiter().GetResult();
#pragma warning restore CA2012
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	protected override ValueTask DisposeAsyncCore() => DisposeAsync(IOBehavior.Asynchronous);

	private async ValueTask DisposeAsync(IOBehavior ioBehavior)
	{
		if (Pool is not null)
		{
			await Pool.ClearAsync(ioBehavior, default).ConfigureAwait(false);
			Pool.Dispose();
		}
		m_isDisposed = true;
	}

	internal ConnectionPool? Pool { get; }

	internal MySqlConnectorLoggingConfiguration LoggingConfiguration { get; }

	private static int s_lastId;

	private readonly ILogger m_logger;
	private readonly int m_id;
	private readonly string m_connectionString;
	private bool m_isDisposed;
}
#endif
