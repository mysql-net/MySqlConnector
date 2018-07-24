using Dapper;

namespace SideBySide
{
	public class DataTypesFixture : DatabaseFixture
	{
		public DataTypesFixture()
		{
			Connection.Open();
			Connection.Execute(@"
drop table if exists datatypes_bools;
create table datatypes_bools(
  rowid integer not null primary key auto_increment,
  Boolean bool null,
  TinyInt1 tinyint(1) null,
  TinyInt1U tinyint(1) unsigned null
);

insert into datatypes_bools(Boolean, TinyInt1, TinyInt1U)
values
  (null, null, null),
  (0, 0, 0),
  (1, 1, 1),
  (false, false, false),
  (true, true, true),
  (-1, -1, 255),
  (123, 123, 123);

drop table if exists datatypes_bits;
create table datatypes_bits(
  rowid integer not null primary key auto_increment,
  Bit1 bit(1) null,
  Bit32 bit(32) null,
  Bit64 bit(64) null
);

insert into datatypes_bits(Bit1, Bit32, Bit64)
values
  (null, null, null),
  (0, 0, 0),
  (1, 1, 1),
  (1, X'FFFFFFFF', X'FFFFFFFFFFFFFFFF');

drop table if exists datatypes_enums;
create table datatypes_enums(
	rowid integer not null primary key auto_increment,
	size enum('x-small', 'small', 'medium', 'large', 'x-large'),
	color enum('red', 'orange', 'yellow', 'green', 'blue', 'indigo', 'violet') not null
);

insert into datatypes_enums(size, color)
values
	(null, 'red'),
	('small', 'orange'),
	('medium', 'green');

drop table if exists datatypes_integers;
create table datatypes_integers (
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

insert into datatypes_integers(SByte, Byte, Int16, UInt16, Int24, UInt24, Int32, UInt32, Int64, UInt64)
values
  (null, null, null, null, null, null, null, null, null, null), # null
  (0, 0, 0, 0, 0, 0, 0, 0, 0, 0), # zero
  (-128, 0, -32768, 0, -8388608, 0, -2147483648, 0, -9223372036854775808, 0), # minimum
  (127, 255, 32767, 65535, 8388607, 16777215, 2147483647, 4294967295, 9223372036854775807, 18446744073709551615), # maximum
  (123, 123, 12345, 12345, 1234567, 1234567, 123456789, 123456789, 1234567890123456789, 1234567890123456789);

drop table if exists datatypes_reals;
create table datatypes_reals(
  rowid integer not null primary key auto_increment,
  Single float null,
  `Double` double null,
  SmallDecimal decimal(5, 2) null,
  MediumDecimal decimal(28, 8) null,
  BigDecimal decimal(50, 30) null
);

insert into datatypes_reals(Single, `Double`, SmallDecimal, MediumDecimal, BigDecimal)
values
  (null, null, null, null, null),
  (0, 0, 0, 0, 0),
  (-3.40282e38, -1.7976931348623157e308, -999.99, -999999999999.99999999, -99999999999999999999.999999999999999999999999999999),
  (-1.401298E-45, -4.94065645841247e-324, -0.01, -0.00000001, -0.000000000000000000000000000001),
  (3.40282e38, 1.7976931348623157e308, 999.99, 999999999999.99999999, 99999999999999999999.999999999999999999999999999999),
  (1.401298E-45, 4.94065645841247e-324, 0.01, 0.00000001, 0.000000000000000000000000000001);

drop table if exists datatypes_set;
create table datatypes_set(
	rowid integer not null primary key auto_increment,
	value set('one', 'two', 'four') null
);

insert into datatypes_set(value)
values
	(null),
	(''),
	('one'),
	('two'),
	('one,two'),
	('four'),
	('one,four'),
	('two,four'),
	('one,two,four');

drop table if exists datatypes_strings;
create table datatypes_strings (
  rowid integer not null primary key auto_increment,
  utf8 varchar(300) character set 'utf8mb4' null,
  utf8bin varchar(300) character set utf8mb4 collate utf8mb4_bin null,
  latin1 varchar(300) character set 'latin1' null,
  latin1bin varchar(300) character set latin1 collate latin1_bin null,
  cp1251 varchar(300) character set 'cp1251' null,
  guid char(36) null,
  guidbin char(36) binary null
);

insert into datatypes_strings(utf8, utf8bin, latin1, latin1bin, cp1251, guid, guidbin)
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

drop table if exists datatypes_blobs;
create table datatypes_blobs(
  rowid integer not null primary key auto_increment,
  `Binary` binary(100) null,
  `VarBinary` varbinary(100) null,
  `TinyBlob` tinyblob null,
  `Blob` blob null,
  `MediumBlob` mediumblob null,
  `LongBlob` longblob null,
  guidbin binary(16) null
);
drop table if exists datatypes_blob_insert;
create table datatypes_blob_insert like datatypes_blobs;

insert into datatypes_blobs(`Binary`, `VarBinary`, `TinyBlob`, `Blob`, `MediumBlob`, `LongBlob`, guidbin)
values
  (null, null, null, null, null, null, null),
  (X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF');

drop table if exists datatypes_times;
create table datatypes_times(
  rowid integer not null primary key auto_increment,
  `Date` date null,
  `DateTime` datetime(6) null,
  `Timestamp` timestamp(6) null,
  `Time` time(6) null,
  `Year` year null);

insert into datatypes_times(`Date`, `DateTime`, `Timestamp`, `Time`, `Year`)
values
  (null, null, null, null, null),
  (date '1000-01-01', timestamp '1000-01-01 00:00:00', timestamp '1970-01-01 00:00:01', time '-838:59:59' , 1901),
  (date '9999-12-31', timestamp '9999-12-31 23:59:59.999999', '2038-01-18 03:14:07.999999', time '838:59:59.000', 2155), -- not actually maximum Timestamp value, due to TZ conversion
  (null, null, null, time '00:00:00', 0),
  (date '2016-04-05', timestamp '2016-04-05 14:03:04.56789', timestamp '2016-04-05 14:03:04.56789', time '14:03:04.56789', 2016);

drop table if exists datatypes_guids;
create table datatypes_guids (
  rowid integer not null primary key auto_increment,
  char38 char(38) null,
  char38bin char(38) binary null,
  `text` text null,
  `blob` blob null
);

insert into datatypes_guids (char38, char38bin, `text`, `blob`)
values
  (null, null, null, null),
  ('0', '0', '0', X'00'),
  ('33221100-5544-7766-8899-aabbccddeeff', '33221100-5544-7766-8899-aabbccddeeff',
    '33221100-5544-7766-8899-aabbccddeeff', X'00112233445566778899AABBCCDDEEFF'),
  ('{33221100-5544-7766-8899-aabbccddeeff}', '{33221100-5544-7766-8899-aabbccddeeff}',
    '{33221100-5544-7766-8899-aabbccddeeff}', X'00112233445566778899AABBCCDDEEFF');

drop table if exists datatypes_geometry;
create table datatypes_geometry (
  rowid integer not null primary key auto_increment,
  `Geometry` geometry null
);

insert into datatypes_geometry (`Geometry`)
values
  (null),
  (ST_GeomFromText('POINT(1 1)')),
  (ST_GeomFromText('LINESTRING(0 0,1 1,2 2)'));
");

			if (AppConfig.SupportsJson)
			{
				Connection.Execute(@"
drop table if exists datatypes_json_core;
create table datatypes_json_core (
  rowid integer not null primary key auto_increment,
  value json null
);

insert into datatypes_json_core (value)
values
  (null),
  ('null'),
  ('true'),
  ('[]'),
  ('[0]'),
  ('[1]'),
  ('0'),
  ('1'),
  ('{}'),
  ('{""a"": ""b""}');
");
			}
			Connection.Close();
		}
	}
}
