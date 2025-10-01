using System.Globalization;
using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector;

public sealed class MySqlCommandBuilder : DbCommandBuilder
{
	public static void DeriveParameters(MySqlCommand command) => DeriveParametersAsync(IOBehavior.Synchronous, command, CancellationToken.None).GetAwaiter().GetResult();
	public static Task DeriveParametersAsync(MySqlCommand command) => DeriveParametersAsync(command?.Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, command!, CancellationToken.None);
	public static Task DeriveParametersAsync(MySqlCommand command, CancellationToken cancellationToken) => DeriveParametersAsync(command?.Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, command!, cancellationToken);

	private static async Task DeriveParametersAsync(IOBehavior ioBehavior, MySqlCommand command, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(command);
		if (command.CommandType != CommandType.StoredProcedure)
			throw new ArgumentException($"MySqlCommand.CommandType must be StoredProcedure not {command.CommandType}", nameof(command));
		if (string.IsNullOrWhiteSpace(command.CommandText))
			throw new ArgumentException("MySqlCommand.CommandText must be set to a stored procedure name", nameof(command));
		if (command.Connection?.State != ConnectionState.Open)
			throw new ArgumentException("MySqlCommand.Connection must be an open connection.", nameof(command));
		if (command.Connection.Session.ServerVersion.Version < ServerVersions.SupportsProcedureCache)
			throw new NotSupportedException($"MySQL Server {command.Connection.Session.ServerVersion.OriginalString} doesn't support INFORMATION_SCHEMA");

		var cachedProcedure = await command.Connection.GetCachedProcedure(command.CommandText!, revalidateMissing: true, ioBehavior, cancellationToken).ConfigureAwait(false);
		if (cachedProcedure is null)
		{
			var name = NormalizedSchema.MustNormalize(command.CommandText!, command.Connection.Database);
			throw new MySqlException($"Procedure or function '{name.Component}' cannot be found in database '{name.Schema}'.");
		}

		command.Parameters.Clear();
		foreach (var cachedParameter in cachedProcedure.Parameters)
		{
			var parameter = command.Parameters.Add("@" + cachedParameter.Name, cachedParameter.MySqlDbType);
			parameter.Direction = cachedParameter.Direction;
			parameter.Size = cachedParameter.Length;
		}
	}

	public MySqlCommandBuilder()
	{
		GC.SuppressFinalize(this);
		QuotePrefix = "`";
		QuoteSuffix = "`";
	}

	public MySqlCommandBuilder(MySqlDataAdapter dataAdapter)
		: this()
	{
		DataAdapter = dataAdapter;
	}

	public new MySqlDataAdapter? DataAdapter
	{
		get => (MySqlDataAdapter?) base.DataAdapter;
		set => base.DataAdapter = value;
	}

	public new MySqlCommand GetDeleteCommand() => (MySqlCommand) base.GetDeleteCommand();
	public new MySqlCommand GetInsertCommand() => (MySqlCommand) base.GetInsertCommand();
	public new MySqlCommand GetUpdateCommand() => (MySqlCommand) base.GetUpdateCommand();

	protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
	{
		((MySqlParameter) parameter).MySqlDbType = (MySqlDbType) row[SchemaTableColumn.ProviderType];
	}

#if NET6_0_OR_GREATER
	protected override string GetParameterName(int parameterOrdinal) => string.Create(CultureInfo.InvariantCulture, $"@p{parameterOrdinal}");
#else
	protected override string GetParameterName(int parameterOrdinal) => FormattableString.Invariant($"@p{parameterOrdinal}");
#endif
	protected override string GetParameterName(string parameterName) => "@" + parameterName;
	protected override string GetParameterPlaceholder(int parameterOrdinal) => GetParameterName(parameterOrdinal);

	protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
	{
		if (adapter is not MySqlDataAdapter mySqlDataAdapter)
			throw new ArgumentException("adapter needs to be a MySqlDataAdapter", nameof(adapter));

		if (adapter == DataAdapter)
			mySqlDataAdapter.RowUpdating -= RowUpdatingHandler;
		else
			mySqlDataAdapter.RowUpdating += RowUpdatingHandler;
	}

	public override string QuoteIdentifier(string unquotedIdentifier) => QuotePrefix + unquotedIdentifier.Replace("`", "``") + QuoteSuffix;

	public override string UnquoteIdentifier(string quotedIdentifier)
	{
		if (quotedIdentifier is ['`', .., '`'])
			quotedIdentifier = quotedIdentifier[1..^1];
		return quotedIdentifier.Replace("``", "`");
	}

	private void RowUpdatingHandler(object sender, MySqlRowUpdatingEventArgs e) => RowUpdatingHandler(e);
}
