using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.Serialization
{
	internal sealed class InitialHandshakePacket
	{
		public ProtocolCapabilities ProtocolCapabilities { get; }

		public byte[] AuthPluginData { get; }
		public byte[] ServerVersion { get; }

		internal InitialHandshakePacket(ByteArrayReader reader)
		{
			reader.ReadByte(c_protocolVersion);
			ServerVersion = reader.ReadNullTerminatedByteString();
			var connectionId = reader.ReadInt32();
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
					Array.Copy(AuthPluginData, concatenated, AuthPluginData.Length);
					Array.Copy(authPluginData2, 0, concatenated, AuthPluginData.Length, authPluginData2.Length);
					AuthPluginData = concatenated;
				}
				byte[] authPluginName = null;
				if (ProtocolCapabilities.HasFlag(ProtocolCapabilities.PluginAuth))
					authPluginName = reader.ReadNullTerminatedByteString();
			}
		}

		const byte c_protocolVersion = 0x0A;
	}
}

