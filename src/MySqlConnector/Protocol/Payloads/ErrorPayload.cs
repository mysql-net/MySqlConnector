using System.Text;
using MySql.Data.MySqlClient;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	// See https://dev.mysql.com/doc/internals/en/packet-ERR_Packet.html
	internal sealed class ErrorPayload
	{
		public int ErrorCode { get; }
		public string State { get; }
		public string Message { get; }

		public MySqlException ToException() => new MySqlException(ErrorCode, State, Message);

		public static ErrorPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(Signature);

			var errorCode = reader.ReadUInt16();
			var stateMarker = Encoding.ASCII.GetString(reader.ReadByteArray(1));
			string state, message;
			if (stateMarker == "#")
			{
				state = Encoding.ASCII.GetString(reader.ReadByteArray(5));
				message = Encoding.UTF8.GetString(reader.ReadByteArray(payload.ArraySegment.Count - 9));
			}
			else
			{
				state = "HY000";
				message = stateMarker + Encoding.UTF8.GetString(reader.ReadByteArray(payload.ArraySegment.Count - 4));
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
