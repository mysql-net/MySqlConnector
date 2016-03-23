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
