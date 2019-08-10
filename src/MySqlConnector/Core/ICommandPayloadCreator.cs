#nullable disable
using System.Collections.Generic;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core
{
	/// <summary>
	/// <see cref="ICommandPayloadCreator"/> creates the data for an "execute query" command for one or more <see cref="IMySqlCommand"/> objects in a command list.
	/// </summary>
	internal interface ICommandPayloadCreator
	{
		/// <summary>
		/// Writes the payload for an "execute query" command to <paramref name="writer"/>.
		/// </summary>
		/// <param name="commandListPosition">The command list and its current position. This will be updated to the position of the next command to write (or past the end if there are no more commands).</param>
		/// <param name="cachedProcedures">A <see cref="CachedProcedure"/> for all the stored procedures in the command list, if any.</param>
		/// <param name="writer">The <see cref="ByteBufferWriter"/> to write the payload to.</param>
		/// <returns><c>true</c> if a command was written; otherwise, <c>false</c> (if there were no more commands in the list).</returns>
		bool WriteQueryCommand(ref CommandListPosition commandListPosition, IDictionary<string, CachedProcedure> cachedProcedures, ByteBufferWriter writer);
	}
}
