using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Tests;

public class MySqlParameterAppendBinaryTests
{
	[Theory]
	[InlineData(DummySByteEnum.SecondValue, MySqlDbType.Byte, new byte[] { 0x11 })]
	[InlineData(DummyByteEnum.SecondValue, MySqlDbType.UByte, new byte[] { 0x11 })]
	[InlineData(DummyShortEnum.SecondValue, MySqlDbType.Int16, new byte[] { 0x22, 0x11 })]
	[InlineData(DummyUShortEnum.SecondValue, MySqlDbType.UInt16, new byte[] { 0x22, 0x11 })]
	[InlineData(DummyIntEnum.SecondValue, MySqlDbType.Int32, new byte[] { 0x44, 0x33, 0x22, 0x11 })]
	[InlineData(DummyUIntEnum.SecondValue, MySqlDbType.UInt32, new byte[] { 0x44, 0x33, 0x22, 0x11 })]
	[InlineData(DummyEnum.SecondValue, MySqlDbType.Int32, new byte[] { 0x01, 0x00, 0x00, 0x00 })]
	[InlineData(DummyLongEnum.SecondValue, MySqlDbType.Int64, new byte[] { 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 })]
	[InlineData(DummyULongEnum.SecondValue, MySqlDbType.UInt64, new byte[] { 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 })]
	public void WriteBinaryEnumType(object value, MySqlDbType expectedMySqlDbType, byte[] expectedBinary)
	{
		var parameter = new MySqlParameter { Value = value };
		var writer = new ByteBufferWriter();
		parameter.AppendBinary(writer, StatementPreparerOptions.None);

		Assert.Equal(parameter.MySqlDbType, expectedMySqlDbType);
		Assert.Equal(writer.Position, expectedBinary.Length);
		Assert.Equal(writer.ArraySegment.ToArray(), expectedBinary);
	}
}
