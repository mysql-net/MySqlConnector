using System.Diagnostics;
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

	[Fact]
	public void CreateQueryAttributesWithoutActivityDoesNotAddTelemetryAttributes()
	{
		var attributes = new MySqlAttributeCollection();
		attributes.SetAttribute("custom", "value");

		var parameters = CreateQueryAttributes(attributes, activity: null);

		var parameter = Assert.Single(parameters);
		Assert.Equal("custom", parameter.ParameterName);
		Assert.Equal("value", parameter.Value);
	}

	[Fact]
	public void CreateQueryAttributesWithNonW3CActivityDoesNotAddTelemetryAttributes()
	{
		var attributes = new MySqlAttributeCollection();
		attributes.SetAttribute("custom", "value");

		using var activity = new Activity("HierarchicalActivity");
		activity.SetIdFormat(ActivityIdFormat.Hierarchical);
		activity.Start();

		var parameters = CreateQueryAttributes(attributes, activity);

		var parameter = Assert.Single(parameters);
		Assert.Equal("custom", parameter.ParameterName);
		Assert.Equal("value", parameter.Value);
	}

	[Fact]
	public void CreateQueryAttributesAddsTraceparentAndTracestate()
	{
		using var activity = new Activity("W3CActivity");
		activity.SetIdFormat(ActivityIdFormat.W3C);
		activity.TraceStateString = "test=value";
		activity.Start();

		var parameters = CreateQueryAttributes(attributes: null, activity);

		Assert.Collection(parameters,
			x =>
			{
				Assert.Equal("traceparent", x.ParameterName);
				Assert.Equal(activity.Id, x.Value);
			},
			x =>
			{
				Assert.Equal("tracestate", x.ParameterName);
				Assert.Equal(activity.TraceStateString, x.Value);
			});
	}

	[Fact]
	public void CreateQueryAttributesUsesExplicitTelemetryAttributesCaseInsensitively()
	{
		var attributes = new MySqlAttributeCollection();
		attributes.SetAttribute("TRACEPARENT", "explicit-traceparent");
		attributes.SetAttribute("TraceState", "explicit-tracestate");
		attributes.SetAttribute("custom", "value");

		using var activity = new Activity("W3CActivity");
		activity.SetIdFormat(ActivityIdFormat.W3C);
		activity.TraceStateString = "test=value";
		activity.Start();

		var parameters = CreateQueryAttributes(attributes, activity);

		Assert.Collection(parameters,
			x =>
			{
				Assert.Equal("TRACEPARENT", x.ParameterName);
				Assert.Equal("explicit-traceparent", x.Value);
			},
			x =>
			{
				Assert.Equal("TraceState", x.ParameterName);
				Assert.Equal("explicit-tracestate", x.Value);
			},
			x =>
			{
				Assert.Equal("custom", x.ParameterName);
				Assert.Equal("value", x.Value);
			});
	}

	private static MySqlParameter[] CreateQueryAttributes(MySqlAttributeCollection attributes, Activity activity) =>
		(MySqlParameter[]) CreateQueryAttributesMethod.Invoke(null, [attributes, activity])!;

	private static MethodInfo CreateQueryAttributesMethod { get; } = typeof(SingleCommandPayloadCreator)
		.GetMethod("CreateQueryAttributes", BindingFlags.NonPublic | BindingFlags.Static)!;
	private static MethodInfo WriteBinaryParametersMethod { get; } = typeof(SingleCommandPayloadCreator)
		.GetMethod("WriteBinaryParameters", BindingFlags.NonPublic | BindingFlags.Static)!;
}
