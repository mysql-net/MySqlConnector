using System;
using System.Text;
using MySql.Data.MySqlClient;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Payloads
{
	// See https://dev.mysql.com/doc/internals/en/packet-ERR_Packet.html
	internal sealed class ErrorPayload
	{
		public int ErrorCode { get; }
		public string State { get; }
		public string Message { get; }

		public MySqlException ToException() => new MySqlException(ErrorCode, State, Message);

		public static ErrorPayload Create(ReadOnlySpan<byte> span)
		{
			var reader = new ByteArrayReader(span);
			reader.ReadByte(Signature);

			var errorCode = reader.ReadUInt16();
			var stateMarker = Encoding.ASCII.GetString(reader.ReadByteString(1));
			string state, message;
			if (stateMarker == "#")
			{
				state = Encoding.ASCII.GetString(reader.ReadByteString(5));
				message = Encoding.UTF8.GetString(reader.ReadByteString(span.Length - 9));
			}
			else
			{
				state = "HY000";
				message = stateMarker + Encoding.UTF8.GetString(reader.ReadByteString(span.Length - 4));
			}
			return new ErrorPayload(errorCode, state, message);
		}

		public const byte Signature = 0xFF;

		private ErrorPayload(int errorCode, string state, string message)
		{
			ErrorCode = errorCode;
			State = state;
			Message = message;
		}
	}
}
