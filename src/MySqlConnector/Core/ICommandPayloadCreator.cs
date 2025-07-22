using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core;

/// <summary>
/// <see cref="ICommandPayloadCreator"/> creates the data for an "execute query" command for one or more <see cref="IMySqlCommand"/> objects in a command list.
/// </summary>
internal interface ICommandPayloadCreator
{
	/// <summary>
	/// Writes any prologue data that needs to be sent before the current command in the command list.
	/// </summary>
	/// <param name="connection">The <see cref="MySqlConnection"/>.</param>
	/// <param name="commandListPosition">The <see cref="CommandListPosition"/> giving the current command and current prepared statement.</param>
	/// <param name="ioBehavior">The IO behavior.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="ValueTask"/> representing the potentially-asynchronous operation.</returns>
	ValueTask WritePrologueAsync(MySqlConnection connection, CommandListPosition commandListPosition, IOBehavior ioBehavior, CancellationToken cancellationToken);

	/// <summary>
	/// Writes the payload for an "execute query" command to <paramref name="writer"/>.
	/// </summary>
	/// <param name="commandListPosition">The command list and its current position. This will be updated to the position of the next command to write (or past the end if there are no more commands).</param>
	/// <param name="cachedProcedures">A <see cref="CachedProcedure"/> for all the stored procedures in the command list, if any.</param>
	/// <param name="writer">The <see cref="ByteBufferWriter"/> to write the payload to.</param>
	/// <param name="appendSemicolon">Whether a statement-separating semicolon should be appended if it's missing.</param>
	/// <returns><c>true</c> if a command was written; otherwise, <c>false</c> (if there were no more commands in the list).</returns>
	bool WriteQueryCommand(ref CommandListPosition commandListPosition, IDictionary<string, CachedProcedure?> cachedProcedures, ByteBufferWriter writer, bool appendSemicolon);
}
