namespace MySql.Data.Serialization
{
	internal static class SerializationUtility
	{
		public static uint ReadUInt32(byte[] buffer, int offset, int count)
		{
			uint value = 0;
			for (int i = 0; i < count; i++)
				value |= ((uint) buffer[offset + i]) << (8 * i);
			return value;
		}

		public static void WriteUInt32(uint value, byte[] buffer, int offset, int count)
		{
			for (int i = 0; i < count; i++)
			{
				buffer[offset + i] = (byte) (value & 0xFF);
				value >>= 8;
			}
		}

		public static int GetBytesPerCharacter(CharacterSet characterSet)
		{
			// not an exhaustive mapping, but should cover commonly-used character sets
			switch (characterSet)
			{
			case CharacterSet.Utf16Binary:
			case CharacterSet.Utf16GeneralCaseInsensitive:
			case CharacterSet.Utf16UnicodeCaseInsensitive:
			case CharacterSet.Utf16leBinary:
				return 2;

			case CharacterSet.Utf8Binary:
			case CharacterSet.Utf8GeneralCaseInsensitive:
			case CharacterSet.Utf8UnicodeCaseInsensitive:
				return 3;

			case CharacterSet.Utf8Mb4Binary:
			case CharacterSet.Utf8Mb4GeneralCaseInsensitive:
			case CharacterSet.Utf8Mb4UnicodeCaseInsensitive:
			case CharacterSet.Utf32Binary:
			case CharacterSet.Utf32GeneralCaseInsensitive:
			case CharacterSet.Utf32UnicodeCaseInsensitive:
				return 4;

			default:
				return 1;
			}
		}
	}
}
