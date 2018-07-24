using System;
using System.Data;
using System.Linq;
using MySqlConnector.Core;
using Xunit;

namespace MySqlConnector.Tests
{
	public class TypeMapperTests
	{
		[Theory]
		[InlineData(typeof(bool), DbType.Boolean)]
		[InlineData(typeof(sbyte), DbType.SByte)]
		[InlineData(typeof(byte), DbType.Byte)]
		[InlineData(typeof(short), DbType.Int16)]
		[InlineData(typeof(ushort), DbType.UInt16)]
		[InlineData(typeof(int), DbType.Int32)]
		[InlineData(typeof(uint), DbType.UInt32)]
		[InlineData(typeof(long), DbType.Int64)]
		[InlineData(typeof(ulong), DbType.UInt64)]
		[InlineData(typeof(decimal), DbType.Decimal)]
		[InlineData(typeof(double), DbType.Double)]
		[InlineData(typeof(float), DbType.Single)]
		[InlineData(typeof(string), DbType.String)]
		[InlineData(typeof(string), DbType.AnsiString)]
		[InlineData(typeof(byte[]), DbType.Binary)]
		[InlineData(typeof(DateTime), DbType.DateTime)]
		[InlineData(typeof(DateTime), DbType.DateTime2)]
		[InlineData(typeof(DateTimeOffset), DbType.DateTimeOffset)]
		[InlineData(typeof(TimeSpan), DbType.Time)]
		[InlineData(typeof(Guid), DbType.Guid)]
		public void DbTypeMappingTest(Type clrType, DbType dbType)
		{
			Assert.Equal(clrType, TypeMapper.Instance.GetDbTypeMapping(dbType).ClrType);
			Assert.Contains(dbType, TypeMapper.Instance.GetDbTypeMapping(clrType).DbTypes);
		}

		[Theory]
		[InlineData(1, DbType.Boolean, true)]
		[InlineData(true, DbType.SByte, (sbyte)1)]
		[InlineData((sbyte)1, DbType.Byte, (byte)1)]
		[InlineData((byte)1, DbType.Int16, (short)1)]
		[InlineData((short)1, DbType.UInt16, (ushort)1)]
		[InlineData((ushort)1, DbType.Int32, 1)]
		[InlineData(1, DbType.UInt32, (uint)1)]
		[InlineData((uint)1, DbType.Int64, (long)1)]
		[InlineData((long)1, DbType.UInt64, (ulong)1)]
		[InlineData((ulong)1, DbType.String, "1")]
		[InlineData((ulong)1, DbType.StringFixedLength, "1")]
		[InlineData((ulong)1, DbType.AnsiString, "1")]
		[InlineData((ulong)1, DbType.AnsiStringFixedLength, "1")]
		public void ConversionTest(object original, DbType dbType, object expected)
		{
			Assert.Equal(expected, TypeMapper.Instance.GetDbTypeMapping(dbType).DoConversion(original));
		}

		[Theory]
		[InlineData("bit", false, 0, DbType.UInt64)]
		[InlineData("tinyint", false, 1, DbType.Boolean)]
		[InlineData("tinyint", true, 1, DbType.Boolean)]
		[InlineData("tinyint", false, 0, DbType.SByte)]
		[InlineData("tinyint", true, 0, DbType.Byte)]
		[InlineData("smallint", false, 0, DbType.Int16)]
		[InlineData("smallint", true, 0, DbType.UInt16)]
		[InlineData("mediumint", false, 0, DbType.Int32)]
		[InlineData("mediumint", true, 0, DbType.UInt32)]
		[InlineData("int", false, 0, DbType.Int32)]
		[InlineData("int", true, 0, DbType.UInt32)]
		[InlineData("bigint", false, 0, DbType.Int64)]
		[InlineData("bigint", true, 0, DbType.UInt64)]
		[InlineData("decimal", false, 0, DbType.Decimal)]
		[InlineData("double", false, 0, DbType.Double)]
		[InlineData("float", false, 0, DbType.Single)]
		[InlineData("char", false, 0, DbType.StringFixedLength)]
		[InlineData("varchar", false, 0, DbType.String)]
		[InlineData("tinytext", false, 0, DbType.String)]
		[InlineData("text", false, 0, DbType.String)]
		[InlineData("mediumtext", false, 0, DbType.String)]
		[InlineData("longtext", false, 0, DbType.String)]
		[InlineData("enum", false, 0, DbType.String)]
		[InlineData("set", false, 0, DbType.String)]
		[InlineData("json", false, 0, DbType.String)]
		[InlineData("binary", false, 0, DbType.Binary)]
		[InlineData("varbinary", false, 0, DbType.Binary)]
		[InlineData("tinyblob", false, 0, DbType.Binary)]
		[InlineData("blob", false, 0, DbType.Binary)]
		[InlineData("mediumblob", false, 0, DbType.Binary)]
		[InlineData("longblob", false, 0, DbType.Binary)]
		[InlineData("point", false, 0, DbType.Binary)]
		[InlineData("linestring", false, 0, DbType.Binary)]
		[InlineData("polygon", false, 0, DbType.Binary)]
		[InlineData("geometry", false, 0, DbType.Binary)]
		[InlineData("multipoint", false, 0, DbType.Binary)]
		[InlineData("multilinestring", false, 0, DbType.Binary)]
		[InlineData("multipolygon", false, 0, DbType.Binary)]
		[InlineData("datetime", false, 0, DbType.DateTime)]
		[InlineData("date", false, 0, DbType.Date)]
		[InlineData("time", false, 0, DbType.Time)]
		[InlineData("timestamp", false, 0, DbType.DateTime)]
		[InlineData("year", false, 0, DbType.Int32)]
		public void ColumnTypeMetadataTest(string columnTypeName, bool unsigned, int length, DbType dbType)
		{
			Assert.Equal(dbType, TypeMapper.Instance.GetDbTypeMapping(columnTypeName, unsigned, length).DbTypes.FirstOrDefault());
			Assert.Equal(dbType, TypeMapper.Instance.GetDbTypeMapping(columnTypeName.ToUpperInvariant(), unsigned, length).DbTypes.FirstOrDefault());
		}
	}
}
