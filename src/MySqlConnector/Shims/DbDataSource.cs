#if !NET7_0_OR_GREATER
namespace System.Data.Common;

public abstract class DbDataSource
#if NETSTANDARD2_1_OR_GREATER
	: IDisposable, IAsyncDisposable
#else
	: IDisposable
#endif
{
	public abstract string ConnectionString { get; }

	public DbConnection CreateConnection() => CreateDbConnection();

	public DbConnection OpenConnection() => OpenDbConnection();

	public ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) =>
		OpenDbConnectionAsync(cancellationToken);

	public DbCommand CreateCommand(string? commandText = null) => CreateDbCommand(commandText);

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCore().ConfigureAwait(false);

		Dispose(disposing: false);
#pragma warning disable CA1816 // Call GC.SuppressFinalize correctly
		GC.SuppressFinalize(this);
#pragma warning restore CA1816
	}

	protected abstract DbConnection CreateDbConnection();

	protected virtual DbConnection OpenDbConnection()
	{
		var connection = CreateDbConnection();

		try
		{
			connection.Open();
			return connection;
		}
		catch
		{
			connection.Dispose();
			throw;
		}
	}

	protected virtual async ValueTask<DbConnection> OpenDbConnectionAsync(CancellationToken cancellationToken = default)
	{
		var connection = CreateDbConnection();

		try
		{
			await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
			return connection;
		}
		catch
		{
#if NETSTANDARD2_1_OR_GREATER
			await connection.DisposeAsync().ConfigureAwait(false);
#else
			connection.Dispose();
#endif
			throw;
		}
	}

	// The shim doesn't support these methods; to use the full DbDataSource the client needs to be on .NET 7.0.
	protected virtual DbCommand CreateDbCommand(string? commandText = null) => throw new NotSupportedException();

	protected virtual void Dispose(bool disposing)
	{
	}

	protected virtual ValueTask DisposeAsyncCore() => default;
}
#endif
