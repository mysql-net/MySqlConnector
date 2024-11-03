using MySqlConnector;

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? "Server=localhost;Username=root;Password=pass";

await using var dataSource = new MySqlDataSourceBuilder(connectionString).Build();

await using var conn = dataSource.CreateConnection();
await conn.OpenAsync();
await using var cmd = new MySqlCommand("SELECT 'Hello World'", conn);
await using var reader = await cmd.ExecuteReaderAsync();
if (!await reader.ReadAsync())
	throw new Exception("ReadAsync returned false");

var value = reader.GetFieldValue<string>(0);
if (value != "Hello World")
	throw new Exception($"Expected 'Hello World'; got '{value}'");

var schema = reader.GetColumnSchema();
if (schema.Count != 1)
	throw new Exception($"Expected 1 column, got {schema.Count}");
if (((MySqlDbColumn) schema[0]).ProviderType != MySqlDbType.VarChar)
	throw new Exception($"Expected column type to be MySqlDbType.VarChar, got {((MySqlDbColumn) schema[0]).ProviderType}");
if (reader.GetFieldType(0) != typeof(string))
	throw new Exception($"Expected column type to be System.String, got {reader.GetFieldType(0)}");

Console.WriteLine("Success");
