using System;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class CachingSha2ServerResponsePayload
	{
		public const byte Signature = 0x01;

		public const byte SuccessSignature = 0x03;

		public const byte FullAuthRequiredSignature = 0x04;

		private CachingSha2ServerResponsePayload(bool succeeded, bool fullAuthRequired)
		{
			Succeeded = succeeded;
			FullAuthRequired = fullAuthRequired;
		}

		public bool Succeeded { get; }

		public bool FullAuthRequired { get; }

		public static CachingSha2ServerResponsePayload Create(ReadOnlySpan<byte> span)
		{
			var reader = new ByteArrayReader(span);
			reader.ReadByte(Signature);
			var secondByte = reader.ReadByte();

			return new CachingSha2ServerResponsePayload(
				secondByte == SuccessSignature,
				secondByte == FullAuthRequiredSignature);
		}
	}
}
