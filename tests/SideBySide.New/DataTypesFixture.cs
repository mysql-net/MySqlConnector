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

create table datatypes.numbers (
  rowid integer not null primary key auto_increment,
  Int32 int null,
  UInt32 int unsigned null
);

insert into datatypes.numbers(Int32, UInt32)
values
  (null, null), # null
  (0, 0), # zero
  (-2147483648, 0), # minimum
  (2147483647, 4294967295), # maximum
  (123456789, 123456789);

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
