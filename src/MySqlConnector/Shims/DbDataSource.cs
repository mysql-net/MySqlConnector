#if !NET7_0_OR_GREATER
namespace System.Data.Common;

public abstract class DbDataSource
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
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

#if NET6_0_OR_GREATER
	public DbBatch CreateBatch() => CreateDbBatch();
#endif

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

	// No need for actual implementations in this compat shim - its only implementation will be MySqlDataSource, which overrides these.
	protected virtual DbConnection OpenDbConnection() => throw new NotSupportedException();
	protected virtual ValueTask<DbConnection> OpenDbConnectionAsync(CancellationToken cancellationToken = default) =>
		throw new NotSupportedException();
	protected virtual DbCommand CreateDbCommand(string? commandText = null) => throw new NotSupportedException();

#if NET6_0_OR_GREATER
	protected virtual DbBatch CreateDbBatch() => throw new NotSupportedException();
#endif

	protected virtual void Dispose(bool disposing)
	{
	}

	protected virtual ValueTask DisposeAsyncCore() => default;
}
#endif
