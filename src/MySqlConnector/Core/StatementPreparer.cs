using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal sealed class StatementPreparer
{
	public StatementPreparerOptions Options { get; }

	public StatementPreparer(string commandText, MySqlParameterCollection? parameters, StatementPreparerOptions options)
	{
		m_commandText = commandText;
		m_parameters = parameters;
		Options = options;
	}

	public ParsedStatements SplitStatements()
	{
		var statements = new List<ParsedStatement>();
		var statementStartEndIndexes = new List<int>();
		var writer = new ByteBufferWriter(m_commandText.Length + 1);
		var parser = new PreparedCommandSqlParser(this, statements, statementStartEndIndexes, writer);
		parser.Parse(m_commandText);
		for (var i = 0; i < statements.Count; i++)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
			statements[i].StatementBytes = writer.ArraySegment[statementStartEndIndexes[i * 2]..statementStartEndIndexes[i * 2 + 1]];
#else
			statements[i].StatementBytes = writer.ArraySegment.Slice(statementStartEndIndexes[i * 2], statementStartEndIndexes[i * 2 + 1] - statementStartEndIndexes[i * 2]);
#endif
		return new ParsedStatements(statements, writer.ToPayloadData());
	}

	public bool ParseAndBindParameters(ByteBufferWriter writer)
	{
		if (!string.IsNullOrWhiteSpace(m_commandText))
		{
			var parser = new ParameterSqlParser(this, writer);
			parser.Parse(m_commandText);
			return parser.IsComplete;
		}
		return true;
	}

	private int GetParameterIndex(string name)
	{
		var index = m_parameters?.NormalizedIndexOf(name) ?? -1;
		if (index == -1 && (Options & StatementPreparerOptions.AllowUserVariables) == 0)
			throw new MySqlException($"Parameter '{name}' must be defined. To use this as a variable, set 'Allow User Variables=true' in the connection string.");
		return index;
	}

	private MySqlParameter GetInputParameter(int index)
	{
		if (index >= (m_parameters?.Count ?? 0))
			throw new MySqlException($"Parameter index {index} is invalid when only {m_parameters?.Count ?? 0} parameter{(m_parameters?.Count == 1 ? " is" : "s are")} defined.");
		var parameter = m_parameters![index];
		if (parameter.Direction != ParameterDirection.Input && (Options & StatementPreparerOptions.AllowOutputParameters) == 0)
			throw new MySqlException($"Only ParameterDirection.Input is supported when CommandType is Text (parameter name: {parameter.ParameterName})");
		return parameter;
	}

	private sealed class ParameterSqlParser : SqlParser
	{
		public ParameterSqlParser(StatementPreparer preparer, ByteBufferWriter writer)
			: base(preparer)
		{
			m_writer = writer;
		}

		public bool IsComplete { get; private set; }

		protected override void OnNamedParameter(int index, int length)
		{
			var parameterIndex = Preparer.GetParameterIndex(Preparer.m_commandText.Substring(index, length));
			if (parameterIndex != -1)
				DoAppendParameter(parameterIndex, index, length);
		}

		protected override void OnPositionalParameter(int index)
		{
			DoAppendParameter(m_currentParameterIndex, index, 1);
			m_currentParameterIndex++;
		}

		private void DoAppendParameter(int parameterIndex, int textIndex, int textLength)
		{
			m_writer.Write(Preparer.m_commandText, m_lastIndex, textIndex - m_lastIndex);
			var parameter = Preparer.GetInputParameter(parameterIndex);
			parameter.AppendSqlString(m_writer, Preparer.Options);
			m_lastIndex = textIndex + textLength;
		}

		protected override void OnParsed(FinalParseStates states)
		{
			m_writer.Write(Preparer.m_commandText, m_lastIndex, Preparer.m_commandText.Length - m_lastIndex);
			if ((states & FinalParseStates.NeedsNewline) == FinalParseStates.NeedsNewline)
				m_writer.Write((byte) '\n');
			if ((states & FinalParseStates.NeedsSemicolon) == FinalParseStates.NeedsSemicolon && (Preparer.Options & StatementPreparerOptions.AppendSemicolon) == StatementPreparerOptions.AppendSemicolon)
				m_writer.Write((byte) ';');
			IsComplete = (states & FinalParseStates.Complete) == FinalParseStates.Complete;
		}

		private readonly ByteBufferWriter m_writer;
		private int m_currentParameterIndex;
		private int m_lastIndex;
	}

	private sealed class PreparedCommandSqlParser : SqlParser
	{
		public PreparedCommandSqlParser(StatementPreparer preparer, List<ParsedStatement> statements, List<int> statementStartEndIndexes, ByteBufferWriter writer)
			: base(preparer)
		{
			m_statements = statements;
			m_statementStartEndIndexes = statementStartEndIndexes;
			m_writer = writer;
		}

		protected override void OnStatementBegin(int index)
		{
			m_statements.Add(new ParsedStatement());
			m_statementStartEndIndexes.Add(m_writer.Position);
			m_writer.Write((byte) CommandKind.StatementPrepare);
			m_lastIndex = index;
		}

		protected override void OnNamedParameter(int index, int length)
		{
			var parameterName = Preparer.m_commandText.Substring(index, length);
			DoAppendParameter(parameterName, -1, index, length);
		}

		protected override void OnPositionalParameter(int index)
		{
			DoAppendParameter(null, m_currentParameterIndex, index, 1);
			m_currentParameterIndex++;
		}

		private void DoAppendParameter(string? parameterName, int parameterIndex, int textIndex, int textLength)
		{
			// write all SQL up to the parameter
			m_writer.Write(Preparer.m_commandText, m_lastIndex, textIndex - m_lastIndex);
			m_lastIndex = textIndex + textLength;

			// replace the parameter with a ? placeholder
			m_writer.Write((byte) '?');

			// store the parameter index
			m_statements[m_statements.Count - 1].ParameterNames.Add(parameterName);
			m_statements[m_statements.Count - 1].NormalizedParameterNames.Add(parameterName == null ? null : MySqlParameter.NormalizeParameterName(parameterName));
			m_statements[m_statements.Count - 1].ParameterIndexes.Add(parameterIndex);
		}

		protected override void OnStatementEnd(int index)
		{
			m_writer.Write(Preparer.m_commandText, m_lastIndex, index - m_lastIndex);
			m_lastIndex = index;
			m_statementStartEndIndexes.Add(m_writer.Position);
		}

		private readonly List<ParsedStatement> m_statements;
		private readonly List<int> m_statementStartEndIndexes;
		private readonly ByteBufferWriter m_writer;
		private int m_currentParameterIndex;
		private int m_lastIndex;
	}

	private readonly string m_commandText;
	private readonly MySqlParameterCollection? m_parameters;
}
