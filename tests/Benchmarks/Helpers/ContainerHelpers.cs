using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using MySqlConnector;
using Testcontainers.MySql;

namespace Benchmarks.Helpers
{
    internal static class ContainerHelpers
    {
        /// <summary>
        /// Wait untill container start
        /// </summary>
        public static async ValueTask WaitContainerStateRunningAsync(this DockerContainer dockerContainer, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            while (true)
            {
                if (dockerContainer.State == TestcontainersStates.Running)
                {
                    break;
                }

                if (sw.Elapsed >= timeout)
                {
                    throw new Exception($"Container start timeout ({timeout}) exceeded, benchmark stopped, current container state is {dockerContainer.State}.");
                }

                await Task.Delay(100);
            }
        }

        /// <summary>
        /// Wait until the container responds to at least one request
        /// </summary>
        public static async ValueTask WaitResponseAsync(this MySqlContainer container, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            while (true)
            {
                if (sw.Elapsed >= timeout)
                {
                    throw new Exception($"MySQL has not responded to any queries in {timeout}. Container state {container.State}");
                }

                var connectionString = container.GetConnectionString();
                try
                {
                    await using (var conn = new MySqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        await using var command = conn.CreateCommand();
                        command.CommandText = "SELECT 1";
                        var result = await command.ExecuteScalarAsync();

                        if ((result is int intValue && intValue == 1) || (result is long longValue && longValue == 1))
                        {
                            break;
                        }
                    }
                }
                catch
                {
                    // игнорим
                }

                await Task.Delay(100);
            }
        }
    }
}
