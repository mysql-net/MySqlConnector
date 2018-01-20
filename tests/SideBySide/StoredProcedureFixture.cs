using Dapper;

namespace SideBySide
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
			Connection.Execute(@"DROP FUNCTION IF EXISTS failing_function;
				CREATE FUNCTION failing_function()
				RETURNS INT
				BEGIN
					DECLARE v1 INT;
					SELECT c1 FROM table_that_does_not_exist INTO v1;
					RETURN v1;
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
			Connection.Execute(@"DROP PROCEDURE IF EXISTS out_string;
				CREATE PROCEDURE out_string(
					OUT value VARCHAR(100)
				)
				BEGIN
					SELECT 'test value' INTO value;
				END");
			Connection.Execute(@"DROP PROCEDURE IF EXISTS out_null;
				CREATE PROCEDURE out_null(
					OUT string_value VARCHAR(100),
					OUT int_value INT
				)
				BEGIN
					SELECT NULL INTO string_value;
					SELECT NULL INTO int_value;
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
			Connection.Execute(@"drop procedure if exists multiple_result_sets;
				create procedure multiple_result_sets (in pivot int)
				begin
					select name from sproc_multiple_rows where value < pivot order by name;
					select name from sproc_multiple_rows where value > pivot order by name;
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
			Connection.Execute(@"drop procedure if exists `dotted.name`;
				create procedure `dotted.name`()
				begin
					select 1, 2, 3;
				end;");

			if (AppConfig.SupportsJson)
			{
				Connection.Execute(@"drop procedure if exists SetJson;
CREATE PROCEDURE `SetJson`(vJson JSON)
BEGIN
	SELECT vJson;
END
");
			}
		}
	}
}
