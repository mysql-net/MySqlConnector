using System;
using System.Collections.Generic;
using System.Text;

namespace MySql.Data.MySqlClient
{
	internal sealed class MySqlStatementPreparer
	{
		public MySqlStatementPreparer(string commandText, MySqlParameterCollection parameters, StatementPreparerOptions options)
		{
			m_commandText = commandText;
			m_parameters = parameters;
			m_options = options;
			m_hasBoundParameters = string.IsNullOrWhiteSpace(m_commandText);

			m_namedParameters = new Dictionary<string, MySqlParameter>(StringComparer.OrdinalIgnoreCase);
			foreach (MySqlParameter parameter in parameters)
			{
				if (!string.IsNullOrWhiteSpace(parameter.ParameterName))
					m_namedParameters.Add(parameter.ParameterName, parameter);
			}
		}

		public void BindParameters()
		{
			// check if already bound
			if (m_hasBoundParameters)
				return;

			var parser = new ParameterSqlParser(this);
			parser.Parse(m_commandText);
			PreparedSql = parser.Output.ToString();

			m_hasBoundParameters = true;
		}

		public string PreparedSql { get; private set; }

		private sealed class ParameterSqlParser : MySqlParser
		{
			public ParameterSqlParser(MySqlStatementPreparer preparer)
			{
				m_preparer = preparer;
				Output = new StringBuilder();
			}

			public StringBuilder Output { get; }

			protected override void OnNamedParameter(int index, int length)
			{
				var parameterName = m_preparer.m_commandText.Substring(index, length);
				var parameterIndex = m_preparer.m_parameters.IndexOf(parameterName);
				if (parameterIndex != -1)
					DoAppendParameter(parameterIndex, index, length);
				else if (!m_preparer.m_options.HasFlag(StatementPreparerOptions.AllowUserVariables))
					throw new MySqlException("Parameter '{0}' must be defined.".FormatInvariant(parameterName));
			}

			protected override void OnPositionalParameter(int index)
			{
				DoAppendParameter(m_currentParameterIndex, index, 1);
				m_currentParameterIndex++;
			}

			private void DoAppendParameter(int parameterIndex, int textIndex, int textLength)
			{
				Output.Append(m_preparer.m_commandText, m_lastIndex, textIndex - m_lastIndex);
				m_preparer.m_parameters[parameterIndex].AppendSqlString(Output);
				m_lastIndex = textIndex + textLength;
			}

			protected override void OnParsed()
			{
				Output.Append(m_preparer.m_commandText, m_lastIndex, m_preparer.m_commandText.Length - m_lastIndex);
			}

			readonly MySqlStatementPreparer m_preparer;
			int m_currentParameterIndex;
			int m_lastIndex;
		}

		readonly string m_commandText;
		readonly MySqlParameterCollection m_parameters;
		private readonly StatementPreparerOptions m_options;
		readonly Dictionary<string, MySqlParameter> m_namedParameters;
		bool m_hasBoundParameters;
	}
}
