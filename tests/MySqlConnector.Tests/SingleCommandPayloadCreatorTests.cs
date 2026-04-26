using System.Reflection;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Tests;

public class SingleCommandPayloadCreatorTests
{
	[Fact]
	public void WriteBinaryParametersPreservesExplicitStringType()
	{
		var server = new FakeMySqlServer();
		server.Start();

		try
		{
			var parameter = new MySqlParameter("traceparent", MySqlDbType.String)
			{
				Value = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
			};
			var connectionStringBuilder = new MySqlConnectionStringBuilder
			{
				Server = "localhost",
				Port = (uint) server.Port,
			};
			using var connection = new MySqlConnection(connectionStringBuilder.ConnectionString);
			connection.Open();
			var command = new MySqlCommand
			{
				Connection = connection,
			};
			var writer = new ByteBufferWriter();

			WriteBinaryParametersMethod.Invoke(null, [writer, new[] { parameter }, command, true, 0]);

			Assert.True(parameter.HasSetDbType);
			Assert.True(writer.Position > 3);
			Assert.Equal((byte) ColumnType.String, writer.ArraySegment.Array![writer.ArraySegment.Offset + 2]);
			Assert.Equal(0, writer.ArraySegment.Array![writer.ArraySegment.Offset + 3]);
		}
		finally
		{
			server.Stop();
		}
	}

	private static MethodInfo WriteBinaryParametersMethod { get; } = typeof(SingleCommandPayloadCreator)
		.GetMethod("WriteBinaryParameters", BindingFlags.NonPublic | BindingFlags.Static)!;
}
