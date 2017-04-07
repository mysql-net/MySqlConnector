using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class DebugOnlyTests : IClassFixture<DatabaseFixture>
	{

		[Theory]
		[InlineData(1u, 3u, 0u, 5u)]
		[InlineData(1u, 3u, 3u, 5u)]
		public async Task ConnectionLifeTime(uint idleTimeout, uint delaySeconds, uint minPoolSize, uint maxPoolSize)
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.Pooling = true;
			csb.MinimumPoolSize = minPoolSize;
			csb.MaximumPoolSize = maxPoolSize;
			csb.ConnectionIdleTimeout = idleTimeout;
			HashSet<int> serverThreadIdsBegin = new HashSet<int>();
			HashSet<int> serverThreadIdsEnd = new HashSet<int>();

			async Task OpenConnections(uint numConnections, HashSet<int> serverIdSet)
			{
				using (var connection = new MySqlConnection(csb.ConnectionString))
				{
					await connection.OpenAsync();
					serverIdSet.Add(connection.ServerThread);
					if (--numConnections <= 0)
						return;
					await OpenConnections(numConnections, serverIdSet);
				}
			}

			await OpenConnections(maxPoolSize, serverThreadIdsBegin);
			await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
			await OpenConnections(maxPoolSize, serverThreadIdsEnd);

			serverThreadIdsEnd.IntersectWith(serverThreadIdsBegin);
			Assert.Equal((int)minPoolSize, serverThreadIdsEnd.Count);
		}

	}
}
