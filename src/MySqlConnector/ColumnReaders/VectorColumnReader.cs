using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class VectorColumnReader : ColumnReader
{
	public static VectorColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (BitConverter.IsLittleEndian)
		{
			return new ReadOnlyMemory<float>(MemoryMarshal.Cast<byte, float>(data).ToArray());
		}
		else
		{
			var floats = new float[data.Length / 4];

#if !NET5_0_OR_GREATER
			var bytes = data.ToArray();
#endif
			for (var i = 0; i < floats.Length; i++)
			{
#if NET5_0_OR_GREATER
				floats[i] = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(i * 4));
#else
				Array.Reverse(bytes, i * 4, 4);
				floats[i] = BitConverter.ToSingle(bytes, i * 4);
#endif
			}

			return new ReadOnlyMemory<float>(floats);
		}
	}
}
