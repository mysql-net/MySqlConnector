using System.Text;
using MySql.Data.MySqlClient;

namespace MySql.Data.Serialization
{
	// See https://dev.mysql.com/doc/internals/en/packet-ERR_Packet.html
	internal class ErrorPayload
	{
		public int ErrorCode { get; }
		public string State { get; }
		public string Message { get; }

		public MySqlException ToException()
		{
			return new MySqlException(ErrorCode, State, Message);
		}

		public static ErrorPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(Signature);

			var errorCode = reader.ReadUInt16();
			reader.ReadByte(0x23);
			var state = Encoding.ASCII.GetString(reader.ReadByteString(5));
			var message = Encoding.UTF8.GetString(reader.ReadByteString(payload.ArraySegment.Count - 9));
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
