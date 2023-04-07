using Microsoft.Extensions.Logging;
using MySqlConnector.Core;

namespace MySqlConnector;

#if !NET6_0_OR_GREATER
#pragma warning disable CA1822 // Mark members as static
#endif

public sealed class MySqlBatchCommand :
#if NET6_0_OR_GREATER
	DbBatchCommand,
#endif
	IMySqlCommand
{
	public MySqlBatchCommand()
		: this(null)
	{
	}

	public MySqlBatchCommand(string? commandText)
	{
		CommandText = commandText ?? "";
		CommandType = CommandType.Text;
	}

#if NET6_0_OR_GREATER
	public override string CommandText { get; set; }
#else
	public string CommandText { get; set; }
#endif
#if NET6_0_OR_GREATER
	public override CommandType CommandType { get; set; }
#else
	public CommandType CommandType { get; set; }
#endif
#if NET6_0_OR_GREATER
	public override int RecordsAffected =>
#else
	public int RecordsAffected =>
#endif
		0;

	public int CommandTimeout => 0;

#if NET6_0_OR_GREATER
	public new MySqlParameterCollection Parameters =>
#else
	public MySqlParameterCollection Parameters =>
#endif
		m_parameterCollection ??= new();

#if NET6_0_OR_GREATER
	protected override DbParameterCollection DbParameterCollection => Parameters;
#endif

	bool IMySqlCommand.AllowUserVariables => false;

	CommandBehavior IMySqlCommand.CommandBehavior => Batch!.CurrentCommandBehavior;

	MySqlParameterCollection? IMySqlCommand.RawParameters => m_parameterCollection;

	MySqlAttributeCollection? IMySqlCommand.RawAttributes => null;

	MySqlConnection? IMySqlCommand.Connection => Batch?.Connection;

	long IMySqlCommand.LastInsertedId => m_lastInsertedId;

	PreparedStatements? IMySqlCommand.TryGetPreparedStatements() => null;

	void IMySqlCommand.SetLastInsertedId(long lastInsertedId) => m_lastInsertedId = lastInsertedId;

	MySqlParameterCollection? IMySqlCommand.OutParameters { get; set; }

	MySqlParameter? IMySqlCommand.ReturnParameter { get; set; }

	ICancellableCommand IMySqlCommand.CancellableCommand => Batch!;
	ILogger IMySqlCommand.Logger => Batch!.Connection!.LoggingConfiguration.CommandLogger;

	internal MySqlBatch? Batch { get; set; }

	private MySqlParameterCollection? m_parameterCollection;
	private long m_lastInsertedId;
}
