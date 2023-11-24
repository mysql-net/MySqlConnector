using MySqlConnector;

var connectionStringBuilder = new MySqlConnectionStringBuilder
{
	Server = "localhost",
	UserID = "root",
	Password = "pass",
};

await using var dataSource = new MySqlDataSourceBuilder(connectionStringBuilder.ConnectionString)
	.UseName("test")
	.Build();

await using var connection = await dataSource.OpenConnectionAsync();
await using var command = connection.CreateCommand();
command.CommandText = "SELECT 1";
await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
	Console.WriteLine(reader.GetValue(0));
}
