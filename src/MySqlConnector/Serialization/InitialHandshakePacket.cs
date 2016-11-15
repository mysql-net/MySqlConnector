using System;
using System.Text;

namespace MySql.Data.Serialization
{
	internal sealed class InitialHandshakePacket
	{
		public ProtocolCapabilities ProtocolCapabilities { get; }

		public byte[] ServerVersion { get; }
		public int ConnectionId { get; }
		public byte[] AuthPluginData { get; }
		public string AuthPluginName { get; }

		internal InitialHandshakePacket(ByteArrayReader reader)
		{
			reader.ReadByte(c_protocolVersion);
			ServerVersion = reader.ReadNullTerminatedByteString();
			ConnectionId = reader.ReadInt32();
			AuthPluginData = reader.ReadByteString(8);
			reader.ReadByte(0);
			var capabilityFlagsLow = reader.ReadUInt16();
			if (reader.BytesRemaining > 0)
			{
				var charSet = (CharacterSet) reader.ReadByte();
				ServerStatus status = (ServerStatus) reader.ReadInt16();
				var capabilityFlagsHigh = reader.ReadUInt16();
				ProtocolCapabilities = (ProtocolCapabilities) (capabilityFlagsHigh << 16 | capabilityFlagsLow);
				var authPluginDataLength = reader.ReadByte();
				var unused = reader.ReadByteString(10);
				if (ProtocolCapabilities.HasFlag(ProtocolCapabilities.SecureConnection) && authPluginDataLength > 0)
				{
					var authPluginData2 = reader.ReadByteString(Math.Max(13, authPluginDataLength - 8));
					var concatenated = new byte[AuthPluginData.Length + authPluginData2.Length];
					Buffer.BlockCopy(AuthPluginData, 0, concatenated, 0, AuthPluginData.Length);
					Buffer.BlockCopy(authPluginData2, 0, concatenated, AuthPluginData.Length, authPluginData2.Length);
					AuthPluginData = concatenated;
				}
				if (ProtocolCapabilities.HasFlag(ProtocolCapabilities.PluginAuth))
					AuthPluginName = Encoding.UTF8.GetString(reader.ReadNullTerminatedByteString());
			}
		}

		const byte c_protocolVersion = 0x0A;
	}
}
