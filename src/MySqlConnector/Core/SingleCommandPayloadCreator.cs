using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class SingleCommandPayloadCreator : ICommandPayloadCreator
	{
		public static bool TryAdvanceToNextCommand(IReadOnlyList<IMySqlCommand> commands, ref int commandIndex, ref int preparedStatementIndex) // TODO: delete
		{
			var command = commands[commandIndex];
			var preparedStatements = command.TryGetPreparedStatements();

			if (preparedStatements is null)
			{
				commandIndex++;
				preparedStatementIndex = 0;
				return commandIndex < commands.Count;
			}
			else
			{
				// advance to next prepared statement or next command
				preparedStatementIndex++;
				if (preparedStatementIndex == preparedStatements.Statements.Count)
				{
					commandIndex++;
					preparedStatementIndex = 0;
					return commandIndex < commands.Count;
				}
				return true;
			}
		}

		public bool WriteQueryCommand(ref CommandListPosition commandListPosition, ByteBufferWriter writer) => WriteSingleQueryCommand(ref commandListPosition, writer);

		public static bool WriteSingleQueryCommand(ref CommandListPosition commandListPosition, ByteBufferWriter writer)
		{
			if (commandListPosition.CommandIndex == commandListPosition.Commands.Count)
				return false;

			var command = commandListPosition.Commands[commandListPosition.CommandIndex];
			var preparedStatements = command.TryGetPreparedStatements();
			if (preparedStatements is null)
			{
				var preparer = new StatementPreparer(command.CommandText, command.Parameters, command.CreateStatementPreparerOptions());
				preparer.ParseAndBindParameters(writer);

				commandListPosition.CommandIndex++;
			}
			else
			{
				var preparedStatement = preparedStatements.Statements[commandListPosition.PreparedStatementIndex];
				var parameterCollection = command.Parameters;

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

				// advance to next prepared statement or next command
				if (++commandListPosition.PreparedStatementIndex == preparedStatements.Statements.Count)
				{
					commandListPosition.CommandIndex++;
					commandListPosition.PreparedStatementIndex = 0;
				}
			}

			return true;
		}
	}
}
