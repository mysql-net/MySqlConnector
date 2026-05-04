#:property TargetFramework=net10.0
#:property ManagePackageVersionsCentrally=false
#:property PackAsTool=false
#:property PublishAot=false
#:package OpenTelemetry@1.15.3
#:package OpenTelemetry.Exporter.OpenTelemetryProtocol@1.15.3
#:project ../../src/MySqlConnector/MySqlConnector.csproj

using System.Diagnostics;
using MySqlConnector;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string defaultServerConnectionString = "Server=127.0.0.1;Port=3306;User ID=root;Password=pass;";
const string defaultDatabaseName = "telemetry_demo";
const string otlpBaseEndpoint = "http://localhost:4318";

var configuredConnectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") ?? defaultServerConnectionString;
var bootstrapConnectionStringBuilder = new MySqlConnectionStringBuilder(configuredConnectionString);
var databaseName = string.IsNullOrWhiteSpace(bootstrapConnectionStringBuilder.Database) ? defaultDatabaseName : bootstrapConnectionStringBuilder.Database;
bootstrapConnectionStringBuilder.Database = "";

var applicationConnectionStringBuilder = new MySqlConnectionStringBuilder(bootstrapConnectionStringBuilder.ConnectionString)
{
	Database = databaseName,
};

var bootstrapConnectionString = bootstrapConnectionStringBuilder.ConnectionString;
var applicationConnectionString = applicationConnectionStringBuilder.ConnectionString;

using var activitySource = new ActivitySource("MySqlConnector.TelemetrySample");
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
	.SetSampler(new AlwaysOnSampler())
	.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MySqlConnector.TelemetrySample"))
	.AddSource("MySqlConnector")
	.AddSource(activitySource.Name)
	.AddOtlpExporter(options =>
	{
		options.Endpoint = new Uri($"{otlpBaseEndpoint}/v1/traces");
		options.Protocol = OtlpExportProtocol.HttpProtobuf;
	})
	.Build();

string? traceId;
string? queryTraceparent;
string? preparedTraceparent;
string?[] batchTraceparents = [null, null];
using (var rootActivity = activitySource.StartActivity("TelemetryScenario", ActivityKind.Internal))
{
	traceId = rootActivity?.TraceId.ToString();

	await using var connection = new MySqlConnection(bootstrapConnectionString);
	await connection.OpenAsync();

	await using (var createDatabaseCommand = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS {databaseName};", connection))
	{
		await createDatabaseCommand.ExecuteNonQueryAsync();
	}

	await connection.ChangeDatabaseAsync(databaseName);

	await using (var setupCommand = new MySqlCommand(
		"""
		CREATE TABLE IF NOT EXISTS trace_demo (
			id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
			message VARCHAR(100) NOT NULL,
			created_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
		);
		""",
		connection))
	{
		await setupCommand.ExecuteNonQueryAsync();
	}

	await using (var queryCommand = new MySqlCommand("SELECT mysql_query_attribute_string('traceparent');", connection))
	{
		queryTraceparent = (string?) await queryCommand.ExecuteScalarAsync();
	}

	await using (var preparedCommand = new MySqlCommand(
		"SELECT @message AS message, mysql_query_attribute_string('traceparent') AS traceparent;",
		connection))
	{
		preparedCommand.Parameters.AddWithValue("@message", $"prepared at {DateTimeOffset.UtcNow:O}");
		await preparedCommand.PrepareAsync();

		await using var reader = await preparedCommand.ExecuteReaderAsync();
		if (await reader.ReadAsync())
			preparedTraceparent = reader.GetString(reader.GetOrdinal("traceparent"));
		else
			preparedTraceparent = null;
	}

	await using (var batch = new MySqlBatch(connection)
	{
		BatchCommands =
		{
			new MySqlBatchCommand("SELECT mysql_query_attribute_string('traceparent') AS traceparent;"),
			new MySqlBatchCommand("SELECT mysql_query_attribute_string('traceparent') AS traceparent;"),
		},
	})
	{
		await using var reader = await batch.ExecuteReaderAsync();
		var batchResultIndex = 0;
		do
		{
			string? batchTraceparent = null;
			if (await reader.ReadAsync())
				batchTraceparent = reader.GetString(reader.GetOrdinal("traceparent"));

			if (batchResultIndex < batchTraceparents.Length)
				batchTraceparents[batchResultIndex] = batchTraceparent;

			batchResultIndex++;
		} while (await reader.NextResultAsync());
	}
}

await MySqlConnection.ClearAllPoolsAsync();

Console.WriteLine($"OTLP base endpoint: {otlpBaseEndpoint}");
Console.WriteLine($"MySQL bootstrap connection: {bootstrapConnectionString}");
Console.WriteLine($"MySQL application connection: {applicationConnectionString}");
Console.WriteLine($"TRACE_ID={traceId ?? "<null>"}");
Console.WriteLine($"COM_QUERY traceparent: {queryTraceparent ?? "<null>"}");
Console.WriteLine($"COM_STMT_EXECUTE traceparent: {preparedTraceparent ?? "<null>"}");
Console.WriteLine($"BATCH[0] traceparent: {batchTraceparents[0] ?? "<null>"}");
Console.WriteLine($"BATCH[1] traceparent: {batchTraceparents[1] ?? "<null>"}");
Console.WriteLine("Waiting 5 seconds for client and server spans to export to Aspire...");

await Task.Delay(TimeSpan.FromSeconds(5));
tracerProvider.ForceFlush();
