using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MySqlConnector.Logging;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core;

internal sealed class SingleCommandPayloadCreator : ICommandPayloadCreator
{
	public static ICommandPayloadCreator Instance { get; } = new SingleCommandPayloadCreator();

	// This is chosen to be something very unlikely to appear as a column name in a user's query. If a result set is read
	// with this as the first column name, the result set will be treated as 'out' parameters for the previous command.
	public static string OutParameterSentinelColumnName => "\uE001\b\x0B";

	public async ValueTask SendCommandPrologueAsync(MySqlConnection connection, CommandListPosition commandListPosition, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// get the current command and check for prepared statements
		var command = commandListPosition.CommandAt(commandListPosition.CommandIndex);
		var preparedStatements = commandListPosition.PreparedStatements ?? command.TryGetPreparedStatements();
		if (preparedStatements is not null)
		{
			// get the current prepared statement; WriteQueryCommand will advance this
			var preparedStatement = preparedStatements.Statements[commandListPosition.PreparedStatementIndex];
			if (preparedStatement.Parameters is { } parameters)
			{
				byte[]? buffer = null;
				try
				{
					// check each parameter
					for (var i = 0; i < parameters.Length; i++)
					{
						// look up this parameter in the command's parameter collection and check if it is a Stream
						// NOTE: full parameter checks will be performed (and throw any necessary exceptions) in WritePreparedStatement
						var parameterName = preparedStatement.Statement.NormalizedParameterNames![i];
						var parameterIndex = parameterName is not null ? (command.RawParameters?.UnsafeIndexOf(parameterName) ?? -1) : preparedStatement.Statement.ParameterIndexes[i];
						if (command.RawParameters is { } rawParameters && parameterIndex >= 0 && parameterIndex < rawParameters.Count && rawParameters[parameterIndex] is { Value: Stream stream and not MemoryStream })
						{
							// seven-byte COM_STMT_SEND_LONG_DATA header: https://dev.mysql.com/doc/dev/mysql-server/latest/page_protocol_com_stmt_send_long_data.html
							const int packetHeaderLength = 7;

							// send almost-full packets, but don't send exactly ProtocolUtility.MaxPacketSize bytes in one payload (as that's ambiguous to whether another packet follows).
							const int maxDataSize = 16_000_000;
							int totalBytesRead;
							while (true)
							{
								buffer ??= ArrayPool<byte>.Shared.Rent(packetHeaderLength + maxDataSize);
								buffer[0] = (byte) CommandKind.StatementSendLongData;
								BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(1), preparedStatement.StatementId);
								BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(5), (ushort) i);

								// keep reading from the stream until we've filled the buffer to send
#if NET7_0_OR_GREATER
								if (ioBehavior == IOBehavior.Synchronous)
									totalBytesRead = stream.ReadAtLeast(buffer.AsSpan(packetHeaderLength, maxDataSize), maxDataSize, throwOnEndOfStream: false);
								else
									totalBytesRead = await stream.ReadAtLeastAsync(buffer.AsMemory(packetHeaderLength, maxDataSize), maxDataSize, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
#else
								totalBytesRead = 0;
								int bytesRead;
								do
								{
									var sizeToRead = maxDataSize - totalBytesRead;
									if (ioBehavior == IOBehavior.Synchronous)
										bytesRead = stream.Read(buffer, packetHeaderLength + totalBytesRead, sizeToRead);
									else
										bytesRead = await stream.ReadAsync(buffer, packetHeaderLength + totalBytesRead, sizeToRead, cancellationToken).ConfigureAwait(false);
									totalBytesRead += bytesRead;
								} while (bytesRead > 0);
#endif

								if (totalBytesRead == 0)
									break;

								// send StatementSendLongData; MySQL Server will keep appending the sent data to the parameter value
								using var payload = new PayloadData(buffer.AsMemory(0, packetHeaderLength + totalBytesRead), isPooled: false);
								await connection.Session.SendAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
							}
						}
					}
				}
				finally
				{
					if (buffer is not null)
						ArrayPool<byte>.Shared.Return(buffer);
				}
			}
		}
	}

	public bool WriteQueryCommand(ref CommandListPosition commandListPosition, IDictionary<string, CachedProcedure?> cachedProcedures, ByteBufferWriter writer, bool appendSemicolon)
	{
		if (commandListPosition.CommandIndex == commandListPosition.CommandCount)
			return false;

		var command = commandListPosition.CommandAt(commandListPosition.CommandIndex);
		commandListPosition.PreparedStatements = command.TryGetPreparedStatements();
		if (commandListPosition.PreparedStatements is null)
		{
			Log.PreparingCommandPayload(command.Logger, command.Connection!.Session.Id, command.CommandText!);

			writer.Write((byte) CommandKind.Query);
			var supportsQueryAttributes = command.Connection!.Session.SupportsQueryAttributes;
			if (supportsQueryAttributes)
			{
				// attribute count
				var attributes = command.RawAttributes;
				writer.WriteLengthEncodedInteger((uint) (attributes?.Count ?? 0));

				// attribute set count (always 1)
				writer.Write((byte) 1);

				if (attributes?.Count > 0)
					WriteBinaryParameters(writer, attributes.Select(static x => x.ToParameter()).ToArray(), command, true, 0);
			}
			else if (command.RawAttributes?.Count > 0)
			{
				Log.QueryAttributesNotSupported(command.Logger, command.Connection!.Session.Id, command.CommandText!);
			}

			WriteQueryPayload(command, cachedProcedures, writer, appendSemicolon, isFirstCommand: true, isLastCommand: true);
			commandListPosition.LastUsedPreparedStatement = null;
			commandListPosition.CommandIndex++;
		}
		else
		{
			writer.Write((byte) CommandKind.StatementExecute);
			commandListPosition.LastUsedPreparedStatement =
				commandListPosition.PreparedStatements.Statements[commandListPosition.PreparedStatementIndex];
			WritePreparedStatement(command, commandListPosition.LastUsedPreparedStatement, writer);

			// advance to next prepared statement or next command
			if (++commandListPosition.PreparedStatementIndex == commandListPosition.PreparedStatements.Statements.Count)
			{
				commandListPosition.CommandIndex++;
				commandListPosition.PreparedStatementIndex = 0;
				commandListPosition.PreparedStatements = null;
			}
		}
		return true;
	}

	/// <summary>
	/// Writes the text of <paramref name="command"/> to <paramref name="writer"/>, encoded in UTF-8.
	/// </summary>
	/// <param name="command">The command.</param>
	/// <param name="cachedProcedures">The cached procedures.</param>
	/// <param name="writer">The output writer.</param>
	/// <param name="appendSemicolon">Whether a statement-separating semicolon should be appended if it's missing.</param>
	/// <param name="isFirstCommand">Whether this command is the first one.</param>
	/// <param name="isLastCommand">Whether this command is the last one.</param>
	/// <returns><c>true</c> if a complete command was written; otherwise, <c>false</c>.</returns>
	public static bool WriteQueryPayload(IMySqlCommand command, IDictionary<string, CachedProcedure?> cachedProcedures, ByteBufferWriter writer, bool appendSemicolon, bool isFirstCommand, bool isLastCommand) =>
		(command.CommandType == CommandType.StoredProcedure) ? WriteStoredProcedure(command, cachedProcedures, writer) : WriteCommand(command, writer, appendSemicolon, isFirstCommand, isLastCommand);

	private static void WritePreparedStatement(IMySqlCommand command, PreparedStatement preparedStatement, ByteBufferWriter writer)
	{
		var parameterCollection = command.RawParameters;

		Log.PreparingCommandPayloadWithId(command.Logger, command.Connection!.Session.Id, preparedStatement.StatementId, command.CommandText!);

		var attributes = command.RawAttributes;
		var supportsQueryAttributes = command.Connection!.Session.SupportsQueryAttributes;
		writer.Write(preparedStatement.StatementId);

		// NOTE: documentation is not updated yet, but due to bugs in MySQL Server 8.0.23-8.0.25, the PARAMETER_COUNT_AVAILABLE (0x08)
		// flag has to be set in the 'flags' block in order for query attributes to be sent with a prepared statement.
		var sendQueryAttributes = supportsQueryAttributes && command.Connection.Session.ServerVersion.Version is not { Major: 8, Minor: 0, Build: >= 23 and <= 25 };
		writer.Write((byte) (sendQueryAttributes ? 8 : 0));
		writer.Write(1);

		var commandParameterCount = preparedStatement.Statement.ParameterNames?.Count ?? 0;
		var attributeCount = attributes?.Count ?? 0;
		if (sendQueryAttributes)
		{
			writer.WriteLengthEncodedInteger((uint) (commandParameterCount + attributeCount));
		}
		else
		{
			if (supportsQueryAttributes && commandParameterCount > 0)
				writer.WriteLengthEncodedInteger((uint) commandParameterCount);
			if (attributeCount > 0)
			{
				Log.QueryAttributesNotSupportedWithId(command.Logger, command.Connection!.Session.Id, preparedStatement.StatementId);
				attributeCount = 0;
			}
		}

		if (commandParameterCount > 0 || attributeCount > 0)
		{
			// TODO: How to handle incorrect number of parameters?

			// build subset of parameters for this statement
			var parameters = new MySqlParameter[commandParameterCount + attributeCount];
			for (var i = 0; i < commandParameterCount; i++)
			{
				var parameterName = preparedStatement.Statement.NormalizedParameterNames![i];
				var parameterIndex = parameterName is not null ? (parameterCollection?.UnsafeIndexOf(parameterName) ?? -1) : preparedStatement.Statement.ParameterIndexes[i];
				if (parameterIndex == -1 && parameterName is not null)
					throw new MySqlException($"Parameter '{preparedStatement.Statement.ParameterNames![i]}' must be defined.");
				else if (parameterIndex < 0 || parameterIndex >= (parameterCollection?.Count ?? 0))
					throw new MySqlException($"Parameter index {parameterIndex} is invalid when only {parameterCollection?.Count ?? 0} parameter{(parameterCollection?.Count == 1 ? " is" : "s are")} defined.");
				parameters[i] = parameterCollection![parameterIndex];
			}
			for (var i = 0; i < attributeCount; i++)
				parameters[commandParameterCount + i] = attributes![i].ToParameter();
			WriteBinaryParameters(writer, parameters, command, supportsQueryAttributes, commandParameterCount);
		}
	}

	private static void WriteBinaryParameters(ByteBufferWriter writer, MySqlParameter[] parameters, IMySqlCommand command, bool supportsQueryAttributes, int parameterCount)
	{
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

		for (var index = 0; index < parameters.Length; index++)
		{
			// override explicit MySqlDbType with inferred type from the Value
			var parameter = parameters[index];
			var mySqlDbType = parameter.MySqlDbType;
			var typeMapping = (parameter.Value is null || parameter.Value == DBNull.Value) ? null : TypeMapper.Instance.GetDbTypeMapping(parameter.Value.GetType());
			if (typeMapping is not null)
			{
				var dbType = typeMapping.DbTypes[0];
				mySqlDbType = TypeMapper.Instance.GetMySqlDbTypeForDbType(dbType);
			}

			// HACK: MariaDB doesn't have a dedicated Vector type so mark it as binary data
			if (mySqlDbType == MySqlDbType.Vector && command.Connection!.Session.ServerVersion.IsMariaDb)
				mySqlDbType = MySqlDbType.LongBlob;

			writer.Write(TypeMapper.ConvertToColumnTypeAndFlags(mySqlDbType, command.Connection!.GuidFormat));

			if (supportsQueryAttributes)
			{
				if (index < parameterCount)
					writer.Write((byte) 0); // empty string
				else
					writer.WriteLengthEncodedString(parameter.ParameterName);
			}
		}

		var options = command.CreateStatementPreparerOptions();
		foreach (var parameter in parameters)
			parameter.AppendBinary(writer, options);
	}

	private static bool WriteStoredProcedure(IMySqlCommand command, IDictionary<string, CachedProcedure?> cachedProcedures, ByteBufferWriter writer)
	{
		var parameterCollection = command.RawParameters;
		var cachedProcedure = cachedProcedures[command.CommandText!];
		if (cachedProcedure is not null)
			parameterCollection = cachedProcedure.AlignParamsWithDb(parameterCollection);

		MySqlParameter? returnParameter = null;
		var outParameters = new MySqlParameterCollection();
		var outParameterNames = new List<string>();
		var inParameters = new MySqlParameterCollection();
		var argParameterNames = new List<string>();
		var inOutSetParameters = "";
		for (var i = 0; i < (parameterCollection?.Count ?? 0); i++)
		{
			var param = parameterCollection![i];
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
					argParameterNames.Add(outName);

					// special handling for GUIDs to ensure that the result set has a type and length that will be autodetected as a GUID
					switch (param.MySqlDbType, param.Size)
					{
						case (MySqlDbType.Guid, 16):
							outParameterNames.Add($"CAST({outName} AS BINARY(16))");
							break;
						case (MySqlDbType.Guid, 32):
							outParameterNames.Add($"CAST({outName} AS CHAR(32))");
							break;
						case (MySqlDbType.Guid, 36):
							outParameterNames.Add($"CAST({outName} AS CHAR(36))");
							break;
						default:
							outParameterNames.Add(outName);
							break;
					}
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
			if (outParameters.Count > 0 && (command.CommandBehavior & CommandBehavior.SchemaOnly) == 0)
			{
				commandText += "SELECT '" + OutParameterSentinelColumnName + "' AS '" + OutParameterSentinelColumnName + "', " + string.Join(", ", outParameterNames);
			}
		}
		else
		{
			commandText = "SELECT " + commandText;
		}
		command.OutParameters = outParameters;
		command.ReturnParameter = returnParameter;

		var preparer = new StatementPreparer(commandText, inParameters, command.CreateStatementPreparerOptions());
		return preparer.ParseAndBindParameters(writer);
	}

	private static bool WriteCommand(IMySqlCommand command, ByteBufferWriter writer, bool appendSemicolon, bool isFirstCommand, bool isLastCommand)
	{
		var isSchemaOnly = (command.CommandBehavior & CommandBehavior.SchemaOnly) != 0;
		var isSingleRow = (command.CommandBehavior & CommandBehavior.SingleRow) != 0;
		if ((isSchemaOnly || isSingleRow) && isFirstCommand)
		{
			writer.Write((command.Connection!.SupportsPerQueryVariables, isSingleRow) switch
			{
				// server doesn't support per-query variables; use multi-statements
				(false, false) => "SET sql_select_limit=0;\n"u8,
				(false, true) => "SET sql_select_limit=1;\n"u8,

				// server supports per-query variables; use SET STATEMENT
				(true, false) => "SET STATEMENT sql_select_limit=0 FOR "u8,
				(true, true) => "SET STATEMENT sql_select_limit=1 FOR "u8,
			});
		}

		var preparer = new StatementPreparer(command.CommandText!, command.RawParameters, command.CreateStatementPreparerOptions() | ((appendSemicolon || isSchemaOnly || isSingleRow) ? StatementPreparerOptions.AppendSemicolon : StatementPreparerOptions.None));
		var isComplete = preparer.ParseAndBindParameters(writer);

		if ((isSchemaOnly || isSingleRow) && isLastCommand && isComplete && !command.Connection!.SupportsPerQueryVariables)
			writer.Write("\nSET sql_select_limit=default;"u8);

		return isComplete;
	}
}
