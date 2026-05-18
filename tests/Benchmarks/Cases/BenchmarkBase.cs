using System;
using System.Threading.Tasks;
using Benchmarks.Helpers;
using DotNet.Testcontainers.Builders;
using MySqlConnector;
using Testcontainers.MySql;

namespace Benchmarks.Cases
{
    public abstract class BenchmarkBase
    {
#pragma warning disable SA1309 // Field names should not begin with underscore
		private MySqlContainer _mysql;
#pragma warning restore SA1309 // Field names should not begin with underscore

#pragma warning disable SA1401 // Fields should be private
		public MySqlDataSource MySqlDataSource;
#pragma warning restore SA1401 // Fields should be private

		public async Task OneTimeSetUp()
        {
            _mysql =
                new MySqlBuilder("mysql:9.7.0")
                .WithUsername("root")
                .WithPassword("dhgvbh73j")
                .WithPortBinding(3306, true)
                .WithAutoRemove(true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(3306))
                .Build();

            await _mysql.StartAsync();
            await _mysql.WaitContainerStateRunningAsync(TimeSpan.FromMinutes(1));
            await _mysql.WaitResponseAsync(TimeSpan.FromMinutes(1));

            await using (var masterConnection = new MySqlConnection(_mysql.GetConnectionString()))
            {
                await masterConnection.OpenAsync();
                await using var createCmd = masterConnection.CreateCommand();
                createCmd.CommandText = $@"
CREATE DATABASE IF NOT EXISTS benchmark;
";
                createCmd.ExecuteNonQuery();
            }

            var builder = new MySqlConnectionStringBuilder(_mysql.GetConnectionString());
            builder.Database = "benchmark";
            builder.AllowLoadLocalInfile = true;

            MySqlDataSource = new MySqlDataSource(builder.ConnectionString);

            await using var connection = await MySqlDataSource.OpenConnectionAsync();
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
SET GLOBAL local_infile = 1;
";
                cmd.ExecuteNonQuery();
            }
        }

		protected async Task OneTimeTearDown()
        {
            var dataSource = MySqlDataSource;
            if (dataSource != null)
            {
                try
                {
                    await dataSource.DisposeAsync();
                }
                catch
                {
                    // ignore
                }
            }

            if (_mysql != null)
            {
                try
                {
                    await _mysql.DisposeAsync();
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
