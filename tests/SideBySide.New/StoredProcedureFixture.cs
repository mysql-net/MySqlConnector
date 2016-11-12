using Dapper;

namespace SideBySide.New
{
	public class StoredProcedureFixture : DatabaseFixture
	{
		public StoredProcedureFixture()
		{
			Connection.Open();
			Connection.Execute(@"DROP FUNCTION IF EXISTS echof;
				CREATE FUNCTION echof(
					name VARCHAR(63)
				) RETURNS VARCHAR(63)
				BEGIN
					RETURN name;
				END");
			Connection.Execute(@"DROP PROCEDURE IF EXISTS echop;
				CREATE PROCEDURE echop(
					IN name VARCHAR(63)
				)
				BEGIN
					SELECT name;
				END");
			Connection.Execute(@"DROP PROCEDURE IF EXISTS circle;
				CREATE PROCEDURE circle(
					IN radius DOUBLE,
					IN height DOUBLE,
					IN name VARCHAR(63),
					OUT diameter DOUBLE,
					OUT circumference DOUBLE,
					OUT area DOUBLE,
					OUT volume DOUBLE,
					OUT shape VARCHAR(63)
				)
				BEGIN
					SELECT radius * 2 INTO diameter;
					SELECT diameter * PI() INTO circumference;
					SELECT PI() * POW(radius, 2) INTO area;
					SELECT area * height INTO volume;
					SELECT 'circle' INTO shape;
					SELECT CONCAT(name, shape);
				END");
			Connection.Execute(@"drop table if exists sproc_multiple_rows;
				create table sproc_multiple_rows (
					value integer not null primary key auto_increment,
					name text not null
				);
				insert into sproc_multiple_rows values
				(1, 'one'),
				(2, 'two'),
				(3, 'three'),
				(4, 'four'),
				(5, 'five'),
				(6, 'six'),
				(7, 'seven'),
				(8, 'eight');");
			Connection.Execute(@"drop procedure if exists number_multiples;
				create procedure number_multiples (in factor int)
				begin
					select name from sproc_multiple_rows
					where mod(value, factor) = 0
					order by name;
				end;");
			Connection.Execute(@"drop procedure if exists number_lister;
				create procedure number_lister (inout high int)
				begin
					DECLARE i int;
					SET i = 1;
					WHILE (i <= high) DO
						select value, name from sproc_multiple_rows
						where value <= high
						order by value;
						SET i = i + 1;
					END WHILE;
					SET high = high + 1;
				end;");
		}
	}
}