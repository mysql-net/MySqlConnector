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
  Int32 int null,
  UInt32 int unsigned null
);

insert into datatypes.numbers(SByte, Byte, Int32, UInt32)
values
  (null, null, null, null), # null
  (0, 0, 0, 0), # zero
  (-128, 0, -2147483648, 0), # minimum
  (127, 255, 2147483647, 4294967295), # maximum
  (123, 123, 123456789, 123456789);

create table datatypes.strings (
  rowid integer not null primary key auto_increment,
  utf8 varchar(100) character set 'utf8mb4' null,
  latin1 varchar(100) character set 'latin1' null,
  cp1251 varchar(100) character set 'cp1251' null
);

insert into datatypes.strings(utf8, latin1, cp1251)
values
  (null, null, null),
  ('', '', ''),
  ('ASCII', 'ASCII', 'ASCII'),
  ('Ũńıċōđĕ', 'Lãtïñ', 'АБВГабвг');

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
