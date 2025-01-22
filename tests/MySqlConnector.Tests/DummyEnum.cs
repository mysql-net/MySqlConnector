namespace MySqlConnector.Tests;

internal enum DummyEnum
{
	FirstValue,
	SecondValue,
}

internal enum DummyByteEnum : byte
{
	FirstValue,
	SecondValue = 0x11,
}

internal enum DummySByteEnum : sbyte
{
	FirstValue,
	SecondValue = 0x11,
}

internal enum DummyShortEnum : short
{
	FirstValue,
	SecondValue = 0x1122,
}

internal enum DummyUShortEnum : ushort
{
	FirstValue,
	SecondValue = 0x1122,
}

internal enum DummyIntEnum : int
{
	FirstValue,
	SecondValue = 0x11223344,
}

internal enum DummyUIntEnum : uint
{
	FirstValue,
	SecondValue = 0x11223344,
}

internal enum DummyLongEnum : long
{
	FirstValue,
	SecondValue = 0x11223344_55667788,
}

internal enum DummyULongEnum : ulong
{
	FirstValue,
	SecondValue = 0x11223344_55667788,
}
