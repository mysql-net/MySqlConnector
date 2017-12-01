using System;
using System.Text;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class InitialHandshakePayload
	{
		public ProtocolCapabilities ProtocolCapabilities { get; }
		public byte[] ServerVersion { get; }
		public int ConnectionId { get; }
		public byte[] AuthPluginData { get; }
		public string AuthPluginName { get; }

		public static InitialHandshakePayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(c_protocolVersion);
			var serverVersion = reader.ReadNullTerminatedByteString();
			var connectionId = reader.ReadInt32();
			byte[] authPluginData = null;
			var authPluginData1 = reader.ReadByteArraySegment(8);
			string authPluginName = null;
			reader.ReadByte(0);
			var protocolCapabilities = (ProtocolCapabilities) reader.ReadUInt16();
			if (reader.BytesRemaining > 0)
			{
				var charSet = (CharacterSet) reader.ReadByte();
				var status = (ServerStatus) reader.ReadInt16();
				var capabilityFlagsHigh = reader.ReadUInt16();
				protocolCapabilities |= (ProtocolCapabilities) (capabilityFlagsHigh << 16);
				var authPluginDataLength = reader.ReadByte();
				var unused = reader.ReadByteArraySegment(10);
				if ((protocolCapabilities & ProtocolCapabilities.SecureConnection) != 0)
				{
					var authPluginData2 = reader.ReadByteArraySegment(Math.Max(13, authPluginDataLength - 8));
					var concatenated = new byte[authPluginData1.Count + authPluginData2.Count];
					Buffer.BlockCopy(authPluginData1.Array, authPluginData1.Offset, concatenated, 0, authPluginData1.Count);
					Buffer.BlockCopy(authPluginData2.Array, authPluginData2.Offset, concatenated, authPluginData1.Count, authPluginData2.Count);
					authPluginData = concatenated;
				}
				if ((protocolCapabilities & ProtocolCapabilities.PluginAuth) != 0)
					authPluginName = Encoding.UTF8.GetString(reader.ReadNullOrEofTerminatedByteString());
			}
			if (authPluginData == null)
			{
				authPluginData = new byte[authPluginData1.Count];
				Buffer.BlockCopy(authPluginData1.Array, authPluginData1.Offset, authPluginData, 0, authPluginData1.Count);
			}

			if (reader.BytesRemaining != 0)
				throw new FormatException("Extra bytes at end of payload.");

			return new InitialHandshakePayload(protocolCapabilities, serverVersion, connectionId, authPluginData, authPluginName);
		}

		private InitialHandshakePayload(ProtocolCapabilities protocolCapabilities, byte[] serverVersion, int connectionId, byte[] authPluginData, string authPluginName)
		{
			ProtocolCapabilities = protocolCapabilities;
			ServerVersion = serverVersion;
			ConnectionId = connectionId;
			AuthPluginData = authPluginData;
			AuthPluginName = authPluginName;
		}

		const byte c_protocolVersion = 0x0A;
	}
}
