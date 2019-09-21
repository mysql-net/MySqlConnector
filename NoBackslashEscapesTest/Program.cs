using System;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using MySql.Data.MySqlClient;

namespace NoBackslashEscapesTest
{
	public class Program
	{
		private const string MySqlConnectorConnectionString = "server=127.0.0.1;user id=root;password=;port=3308";
		private const string ConnectorOdbcConnectionString = "Driver={MySQL ODBC 8.0 Unicode Driver};server=127.0.0.1;user=root;password=;port=3308";

		private static Func<DbConnection> _dbConnectionFactory;

		public static void Main()
		{
			RunMySqlConnectorTests();

			// Debug.Assert(!Environment.Is64BitProcess);
			// RunConnectorOdbcTests();
		}

		private static void RunMySqlConnectorTests()
		{
			_dbConnectionFactory = () => new MySqlConnection(MySqlConnectorConnectionString);
			RunTests();
		}

		private static void RunConnectorOdbcTests()
		{
			_dbConnectionFactory = () => new OdbcConnection(ConnectorOdbcConnectionString);
			RunTests();
		}

		private static void RunTests()
		{
			Succeeds_with_string_literal_backslash_only();
			Succeeds_with_string_literal_backslashes_and_quotes();

			Fails_with_string_parameter_backslashes_only();
			Fails_with_string_parameter_backslashes_and_quotes();
		}

		private static void Succeeds_with_string_literal_backslash_only()
		{
			Console.WriteLine("Succeeds_with_string_literal_backslash_only");

			// Returns 2 backslashes between two spaces on each side: "  \\  "

			string result;

			try
			{
				result = QuerySingleValue<string>(@"select '  \\  '", prepare: false);
				Debug.Assert(result == @"  \\  ");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed with " + e.GetType().Name);
				Debugger.Break();
			}
		}

		private static void Succeeds_with_string_literal_backslashes_and_quotes()
		{
			Console.WriteLine("Succeeds_with_string_literal_backslash_only");

			// Returns "  \'\'  "

			string result;

			try
			{
				result = QuerySingleValue<string>(@"select '  \''\''  '", prepare: false);
				Debug.Assert(result == @"  \'\'  ");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed with " + e.GetType().Name);
				Debugger.Break();
			}
		}

		private static void Fails_with_string_parameter_backslashes_only()
		{
			Console.WriteLine("Succeeds_with_string_literal_backslash_only");

			// Should return 2 backslashes between two spaces on each side: "  \\  "
			//
			// Instead returns 4: "  \\\\  "
			//
			// Check against executing this script directly (mysql.exe or Workbench):
			/*
set session sql_mode = concat(@@sql_mode, ',', 'NO_BACKSLASH_ESCAPES');
set @p0 = '  \\  ';
select @p0;
			*/

			string result;

			try
			{
				result = QuerySingleValue<string>(@"select @p0", prepare: false, parameter: @"  \\  ");
				Debug.Assert(result == @"  \\  ");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed with " + e.GetType().Name);
				Debugger.Break();
			}
		}

		private static void Fails_with_string_parameter_backslashes_and_quotes()
		{
			Console.WriteLine("Succeeds_with_string_literal_backslash_only");

			// Should return: "  \'\'  "
			//
			// Instead gets translated to the command: select '\'\'
			// and then fails with a syntax error.
			//
			// Check against executing this script directly (mysql.exe or Workbench):
			/*
set session sql_mode = concat(@@sql_mode, ',', 'NO_BACKSLASH_ESCAPES');
set @p0 = "  \'\'  ";
select @p0;
		    */

			string result;

			try
			{
				result = QuerySingleValue<string>(@"select @p0", prepare: false, parameter: @"  \'\'  ");
				Debug.Assert(result == @"  \'\'  ");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed with " + e.GetType().Name);
				Debugger.Break();
			}
		}

		public static T QuerySingleValue<T>(string commandText, bool prepare, string parameter = null)
		{
			using (var connection = _dbConnectionFactory())
			{
				connection.Open();

				ExecuteNonQuery(connection, @"set session sql_mode = concat(@@sql_mode, ',', 'NO_BACKSLASH_ESCAPES')");

				var result = ExecuteScalar<T>(connection, commandText, prepare, parameter);

				return result;
			}
		}

		public static void ExecuteNonQuery(DbConnection connection, string commandText)
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = commandText;
				command.ExecuteNonQuery();
			}
		}

		private static T ExecuteScalar<T>(DbConnection connection, string commandText, bool prepare, string parameterValue)
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = commandText;

				if (parameterValue != null)
				{
					var parameter = command.CreateParameter();

					parameter.ParameterName = "@p0";
					parameter.Value = parameterValue;

					command.Parameters.Add(parameter);
				}

				if (prepare)
					command.Prepare();

				var result = command.ExecuteScalar();

				return (T) Convert.ChangeType(result, typeof(T));
			}
		}
	}
}
