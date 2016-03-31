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

create table datatypes.numbers (
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

insert into datatypes.numbers(SByte, Byte, Int16, UInt16, Int24, UInt24, Int32, UInt32, Int64, UInt64)
values
  (null, null, null, null, null, null, null, null, null, null), # null
  (0, 0, 0, 0, 0, 0, 0, 0, 0, 0), # zero
  (-128, 0, -32768, 0, -8388608, 0, -2147483648, 0, -9223372036854775808, 0), # minimum
  (127, 255, 32767, 65535, 8388607, 16777215, 2147483647, 4294967295, 9223372036854775807, 18446744073709551615), # maximum
  (123, 123, 12345, 12345, 1234567, 1234567, 123456789, 123456789, 1234567890123456789, 1234567890123456789);

create table datatypes.strings (
  rowid integer not null primary key auto_increment,
  utf8 varchar(100) character set 'utf8mb4' null,
  utf8bin varchar(100) character set utf8mb4 collate utf8mb4_bin null,
  latin1 varchar(100) character set 'latin1' null,
  latin1bin varchar(100) character set latin1 collate latin1_bin null,
  cp1251 varchar(100) character set 'cp1251' null
);

insert into datatypes.strings(utf8, utf8bin, latin1, latin1bin, cp1251)
values
  (null, null, null, null, null),
  ('', '', '', '', ''),
  ('ASCII', 'ASCII', 'ASCII', 'ASCII', 'ASCII'),
  ('Ũńıċōđĕ', 'Ũńıċōđĕ', 'Lãtïñ', 'Lãtïñ', 'АБВГабвг');

create table datatypes.blobs(
  rowid integer not null primary key auto_increment,
  `Binary` binary(100) null,
  `VarBinary` varbinary(100) null,
  `TinyBlob` tinyblob null,
  `Blob` blob null,
  `MediumBlob` mediumblob null,
  `LongBlob` longblob null
);

insert into datatypes.blobs(`Binary`, `VarBinary`, `TinyBlob`, `Blob`, `MediumBlob`, `LongBlob`)
values
  (null, null, null, null, null, null),
  (X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF',
    X'00112233445566778899AABBCCDDEEFF');
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
