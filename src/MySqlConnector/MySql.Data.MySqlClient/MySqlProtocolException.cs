using System;
using MySqlConnector.Utilities;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlProtocolException : InvalidOperationException
	{
		internal static MySqlProtocolException CreateForPacketOutOfOrder(int expectedSequenceNumber, int packetSequenceNumber)
		{
			return new MySqlProtocolException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(expectedSequenceNumber, packetSequenceNumber));
		}

		private MySqlProtocolException(string message)
			: base(message)
		{
		}
	}
}
