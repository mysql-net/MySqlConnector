using System;
using MySqlConnector.Utilities;

namespace MySqlConnector
{
	/// <summary>
	/// <see cref="MySqlProtocolException"/> is thrown when there is an internal protocol error communicating with MySQL Server.
	/// </summary>
	public sealed class MySqlProtocolException : InvalidOperationException
	{
		/// <summary>
		/// Creates a new <see cref="MySqlProtocolException"/> for an out-of-order packet.
		/// </summary>
		/// <param name="expectedSequenceNumber">The expected packet sequence number.</param>
		/// <param name="packetSequenceNumber">The actual packet sequence number.</param>
		/// <returns>A new <see cref="MySqlProtocolException"/>.</returns>
		internal static MySqlProtocolException CreateForPacketOutOfOrder(int expectedSequenceNumber, int packetSequenceNumber) =>
			new MySqlProtocolException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(expectedSequenceNumber, packetSequenceNumber));

		private MySqlProtocolException(string message)
			: base(message)
		{
		}
	}
}
