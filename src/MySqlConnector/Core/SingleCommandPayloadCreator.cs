using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using MySqlConnector.Logging;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class SingleCommandPayloadCreator : ICommandPayloadCreator
	{
		public static ICommandPayloadCreator Instance { get; } = new SingleCommandPayloadCreator();

		public static string OutParameterSentinelColumnName => "\uE001\b";

		public bool WriteQueryCommand(ref CommandListPosition commandListPosition, IDictionary<string, CachedProcedure> cachedProcedures, ByteBufferWriter writer)
		{
			if (commandListPosition.CommandIndex == commandListPosition.Commands.Count)
				return false;

			var command = commandListPosition.Commands[commandListPosition.CommandIndex];
			var preparedStatements = command.TryGetPreparedStatements();
			if (preparedStatements is null)
			{
				if (Log.IsDebugEnabled())
					Log.Debug("Session{0} Preparing command payload; CommandText: {1}", command.Connection.Session.Id, command.CommandText);

				if (command.CommandType == CommandType.StoredProcedure)
					WriteStoredProcedure(command, cachedProcedures, writer);
				else
					WriteCommand(command, writer);

				commandListPosition.CommandIndex++;
			}
			else
			{
				WritePreparedStatement(command, preparedStatements.Statements[commandListPosition.PreparedStatementIndex], writer);

				// advance to next prepared statement or next command
				if (++commandListPosition.PreparedStatementIndex == preparedStatements.Statements.Count)
				{
					commandListPosition.CommandIndex++;
					commandListPosition.PreparedStatementIndex = 0;
				}
			}

			return true;
		}

		private void WritePreparedStatement(IMySqlCommand command, PreparedStatement preparedStatement, ByteBufferWriter writer)
		{
			var parameterCollection = command.RawParameters;

			if (Log.IsDebugEnabled())
				Log.Debug("Session{0} Preparing command payload; CommandId: {1}; CommandText: {2}", command.Connection.Session.Id, preparedStatement.StatementId, command.CommandText);

			writer.Write((byte) CommandKind.StatementExecute);
			writer.Write(preparedStatement.StatementId);
			writer.Write((byte) 0);
			writer.Write(1);
			if (preparedStatement.Parameters?.Length > 0)
			{
				// TODO: How to handle incorrect number of parameters?

				// build subset of parameters for this statement
				var parameters = new MySqlParameter[preparedStatement.Statement.ParameterNames.Count];
				for (var i = 0; i < preparedStatement.Statement.ParameterNames.Count; i++)
				{
					var parameterName = preparedStatement.Statement.ParameterNames[i];
					var parameterIndex = parameterName != null ? (parameterCollection?.NormalizedIndexOf(parameterName) ?? -1) : preparedStatement.Statement.ParameterIndexes[i];
					if (parameterIndex == -1 && parameterName != null)
						throw new MySqlException("Parameter '{0}' must be defined.".FormatInvariant(parameterName));
					else if (parameterIndex < 0 || parameterIndex >= (parameterCollection?.Count ?? 0))
						throw new MySqlException("Parameter index {0} is invalid when only {1} parameter{2} defined.".FormatInvariant(parameterIndex, parameterCollection?.Count ?? 0, parameterCollection?.Count == 1 ? " is" : "s are"));
					parameters[i] = parameterCollection[parameterIndex];
				}

				// write null bitmap
				byte nullBitmap = 0;
				for (var i = 0; i < parameters.Length; i++)
				{
					var parameter = parameters[i];
					if (parameter.Value is null || parameter.Value == DBNull.Value)
						nullBitmap |= (byte) (1 << (i % 8));

					if (i % 8 == 7)
					{
						writer.Write(nullBitmap);
						nullBitmap = 0;
					}
				}
				if (parameters.Length % 8 != 0)
					writer.Write(nullBitmap);

				// write "new parameters bound" flag
				writer.Write((byte) 1);

				foreach (var parameter in parameters)
					writer.Write(TypeMapper.ConvertToColumnTypeAndFlags(parameter.MySqlDbType, command.Connection.GuidFormat));

				var options = command.CreateStatementPreparerOptions();
				foreach (var parameter in parameters)
					parameter.AppendBinary(writer, options);
			}
		}

		private void WriteStoredProcedure(IMySqlCommand command, IDictionary<string, CachedProcedure> cachedProcedures, ByteBufferWriter writer)
		{
			var parameterCollection = command.RawParameters;
			var cachedProcedure = cachedProcedures[command.CommandText];
			if (cachedProcedure is object)
				parameterCollection = cachedProcedure.AlignParamsWithDb(parameterCollection);

			MySqlParameter returnParameter = null;
			var outParameters = new MySqlParameterCollection();
			var outParameterNames = new List<string>();
			var inParameters = new MySqlParameterCollection();
			var argParameterNames = new List<string>();
			var inOutSetParameters = "";
			for (var i = 0; i < (parameterCollection?.Count ?? 0); i++)
			{
				var param = parameterCollection[i];
				var inName = "@inParam" + i;
				var outName = "@outParam" + i;
				switch (param.Direction)
				{
				case ParameterDirection.Input:
				case ParameterDirection.InputOutput:
					var inParam = param.WithParameterName(inName);
					inParameters.Add(inParam);
					if (param.Direction == ParameterDirection.InputOutput)
					{
						inOutSetParameters += $"SET {outName}={inName}; ";
						goto case ParameterDirection.Output;
					}
					argParameterNames.Add(inName);
					break;
				case ParameterDirection.Output:
					outParameters.Add(param);
					outParameterNames.Add(outName);
					argParameterNames.Add(outName);
					break;
				case ParameterDirection.ReturnValue:
					returnParameter = param;
					break;
				}
			}

			// if a return param is set, assume it is a function; otherwise, assume stored procedure
			var commandText = command.CommandText + "(" + string.Join(", ", argParameterNames) + ");";
			if (returnParameter is null)
			{
				commandText = inOutSetParameters + "CALL " + commandText;
				if (outParameters.Count > 0)
				{
					commandText += "SELECT '"+ OutParameterSentinelColumnName +"' AS '" + OutParameterSentinelColumnName + "', " + string.Join(", ", outParameterNames);
				}
			}
			else
			{
				commandText = "SELECT " + commandText;
			}
			command.OutParameters = outParameters;
			command.ReturnParameter = returnParameter;

			var preparer = new StatementPreparer(commandText, inParameters, command.CreateStatementPreparerOptions());
			preparer.ParseAndBindParameters(writer);
		}

		private void WriteCommand(IMySqlCommand command, ByteBufferWriter writer)
		{
			var preparer = new StatementPreparer(command.CommandText, command.RawParameters, command.CreateStatementPreparerOptions());
			preparer.ParseAndBindParameters(writer);
		}

		static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(SingleCommandPayloadCreator));
	}
}
