using Dapper;

namespace SideBySide
{
	public class DataTypesFixture : DatabaseFixture
	{
		public DataTypesFixture()
		{
			Connection.Open();
			Connection.Execute(@"
drop schema if exists datatypes;

create schema datatypes;

create table datatypes.bools(
  rowid integer not null primary key auto_increment,
  Boolean bool null,
  TinyInt1 tinyint(1) null
);

insert into datatypes.bools(Boolean, TinyInt1)
values
  (null, null),
  (0, 0),
  (1, 1),
  (false, false),
  (true, true),
  (-1, -1),
  (123, 123);

create table datatypes.bits(
  rowid integer not null primary key auto_increment,
  Bit1 bit(1) null,
  Bit32 bit(32) null,
  Bit64 bit(64) null
);

insert into datatypes.bits(Bit1, Bit32, Bit64)
values
  (null, null, null),
  (0, 0, 0),
  (1, 1, 1),
  (1, X'FFFFFFFF', X'FFFFFFFFFFFFFFFF');

create table datatypes.integers (
  rowid integer not null primary key auto_increment,
  SByte tinyint null,
  Byte tinyint unsigned null,
  Int16 smallint null,
  UInt16 smallint unsigned null,
  Int24 mediumint null,
  UInt24 mediumint unsigned null,
  Int32 int null,
  UInt32 int unsigned null,
  Int64 bigint null,
  UInt64 bigint unsigned null
);

insert into datatypes.integers(SByte, Byte, Int16, UInt16, Int24, UInt24, Int32, UInt32, Int64, UInt64)
values
  (null, null, null, null, null, null, null, null, null, null), # null
  (0, 0, 0, 0, 0, 0, 0, 0, 0, 0), # zero
  (-128, 0, -32768, 0, -8388608, 0, -2147483648, 0, -9223372036854775808, 0), # minimum
  (127, 255, 32767, 65535, 8388607, 16777215, 2147483647, 4294967295, 9223372036854775807, 18446744073709551615), # maximum
  (123, 123, 12345, 12345, 1234567, 1234567, 123456789, 123456789, 1234567890123456789, 1234567890123456789);

create table datatypes.reals(
  rowid integer not null primary key auto_increment,
  Single float null,
  `Double` double null,
  SmallDecimal decimal(5, 2) null,
  MediumDecimal decimal(28, 8) null,
  BigDecimal decimal(50, 30) null
);

insert into datatypes.reals(Single, `Double`, SmallDecimal, MediumDecimal, BigDecimal)
values
  (null, null, null, null, null),
  (0, 0, 0, 0, 0),
  (-3.402823466e38, -1.7976931348623157e308, -999.99, -999999999999.99999999, -99999999999999999999.999999999999999999999999999999),
  (-1.401298E-45, -4.94065645841247e-324, -0.01, -0.00000001, -0.000000000000000000000000000001),
  (3.402823466e38, 1.7976931348623157e308, 999.99, 999999999999.99999999, 99999999999999999999.999999999999999999999999999999),
  (1.401298E-45, 4.94065645841247e-324, 0.01, 0.00000001, 0.000000000000000000000000000001);

create table datatypes.strings (
  rowid integer not null primary key auto_increment,
  utf8 varchar(300) character set 'utf8mb4' null,
  utf8bin varchar(300) character set utf8mb4 collate utf8mb4_bin null,
  latin1 varchar(300) character set 'latin1' null,
  latin1bin varchar(300) character set latin1 collate latin1_bin null,
  cp1251 varchar(300) character set 'cp1251' null,
  guid char(36) null,
  guidbin char(36) binary null
);

insert into datatypes.strings(utf8, utf8bin, latin1, latin1bin, cp1251, guid, guidbin)
values
  (null, null, null, null, null, null, null),
  ('', '', '', '', '', '00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000'),
  ('ASCII', 'ASCII', 'ASCII', 'ASCII', 'ASCII', '00000000-0000-0000-c000-000000000046', '00000000-0000-0000-c000-000000000046'),
  ('Ũńıċōđĕ', 'Ũńıċōđĕ', 'Lãtïñ', 'Lãtïñ', 'АБВГабвг', 'fd24a0e8-c3f2-4821-a456-35da2dc4bb8f', 'fd24a0e8-c3f2-4821-a456-35da2dc4bb8f'),
  ('This string has exactly 251 characters in it. The encoded length is stored as 0xFC 0xFB 0x00. 0xFB (i.e., 251) is the sentinel byte indicating ""this field is null"". Incorrectly interpreting the (decoded) length as the sentinel byte would corrupt data.',
   'This string has exactly 251 characters in it. The encoded length is stored as 0xFC 0xFB 0x00. 0xFB (i.e., 251) is the sentinel byte indicating ""this field is null"". Incorrectly interpreting the (decoded) length as the sentinel byte would corrupt data.',
   'This string has exactly 251 characters in it. The encoded length is stored as 0xFC 0xFB 0x00. 0xFB (i.e., 251) is the sentinel byte indicating ""this field is null"". Incorrectly interpreting the (decoded) length as the sentinel byte would corrupt data.',
   'This string has exactly 251 characters in it. The encoded length is stored as 0xFC 0xFB 0x00. 0xFB (i.e., 251) is the sentinel byte indicating ""this field is null"". Incorrectly interpreting the (decoded) length as the sentinel byte would corrupt data.',
   'This string has exactly 251 characters in it. The encoded length is stored as 0xFC 0xFB 0x00. 0xFB (i.e., 251) is the sentinel byte indicating ""this field is null"". Incorrectly interpreting the (decoded) length as the sentinel byte would corrupt data.',
   '6a0e0a40-6228-11d3-a996-0050041896c8', '6a0e0a40-6228-11d3-a996-0050041896c8');

create table datatypes.blobs(
  rowid integer not null primary key auto_increment,
  `Binary` binary(100) null,
  `VarBinary` varbinary(100) null,
  `TinyBlob` tinyblob null,
  `Blob` blob null,
  `MediumBlob` mediumblob null,
  `LongBlob` longblob null,
  guidbin binary(16) null
);

insert into datatypes.blobs(`Binary`, `VarBinary`, `TinyBlob`, `Blob`, `MediumBlob`, `LongBlob`, guidbin)
values
  (null, null, null, null, null, null, null),
  (X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF');

create table datatypes.times(
  rowid integer not null primary key auto_increment,
  `Date` date null,
  `DateTime` datetime(6) null,
  `Timestamp` timestamp(6) null,
  `Time` time(6) null,
  `Year` year null);

insert into datatypes.times(`Date`, `DateTime`, `Timestamp`, `Time`, `Year`)
values
  (null, null, null, null, null),
  (date '1000-01-01', timestamp '1000-01-01 00:00:00', timestamp '1970-01-01 00:00:01', time '-838:59:59' , 1901),
  (date '9999-12-31', timestamp '9999-12-31 23:59:59.999999', '2038-01-18 03:14:07.999999', time '838:59:59.000', 2155), -- not actually maximum Timestamp value, due to TZ conversion
  (date '0000-00-00', timestamp '0000-00-00 00:00:00' , timestamp '0000-00-00 00:00:00', time '00:00:00', 0),
  (date '2016-04-05', timestamp '2016-04-05 14:03:04.56789', timestamp '2016-04-05 14:03:04.56789', time '14:03:04.56789', 2016);
");
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				// Connection.Execute("drop schema datatypes;");
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
	}
}
