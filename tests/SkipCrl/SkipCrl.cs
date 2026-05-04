#:property TargetFramework=net10.0
#:property ManagePackageVersionsCentrally=false
#:property PackAsTool=false
#:property PublishAot=false
#:project ../../src/MySqlConnector/MySqlConnector.csproj

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MySqlConnector;

var configuredConnectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
var caCertificatePath = Environment.GetEnvironmentVariable("CRL_CA_CERT") ??
	Path.Combine(Environment.CurrentDirectory, "tests", "SkipCrl", "generated", "ca-cert.pem");
var serverCertificatePath = Environment.GetEnvironmentVariable("CRL_SERVER_CERT") ??
	Path.Combine(Environment.CurrentDirectory, "tests", "SkipCrl", "generated", "server-cert.pem");
var useTrustStore = string.Equals(Environment.GetEnvironmentVariable("CRL_USE_TRUST_STORE"), "true", StringComparison.OrdinalIgnoreCase);

var baseConnectionStringBuilder = string.IsNullOrWhiteSpace(configuredConnectionString) ?
	new MySqlConnectionStringBuilder
	{
		Server = "localhost",
		Port = 3308,
		UserID = "root",
		Password = "pass",
		Database = "mysql",
		Pooling = false,
		ConnectionTimeout = 10,
	} :
	new MySqlConnectionStringBuilder(configuredConnectionString);

if (!useTrustStore && string.IsNullOrWhiteSpace(baseConnectionStringBuilder.SslCa))
	baseConnectionStringBuilder.SslCa = caCertificatePath;

Console.WriteLine($"Using trust store: {useTrustStore}");
Console.WriteLine($"CA certificate: {caCertificatePath}");
Console.WriteLine($"CA certificate exists: {File.Exists(caCertificatePath)}");
Console.WriteLine($"Server certificate: {serverCertificatePath}");
Console.WriteLine($"Server certificate exists: {File.Exists(serverCertificatePath)}");
Console.WriteLine($"Base connection string: {baseConnectionStringBuilder.ConnectionString}");
Console.WriteLine();

var onlineChainSucceeded = RunChainCheck(caCertificatePath, serverCertificatePath, X509RevocationMode.Online);
Console.WriteLine();
var noCheckChainSucceeded = RunChainCheck(caCertificatePath, serverCertificatePath, X509RevocationMode.NoCheck);
Console.WriteLine();

var verifyCaSucceeded = await TryOpenAsync(baseConnectionStringBuilder, MySqlSslMode.VerifyCA);
Console.WriteLine();
var verifyFullSucceeded = await TryOpenAsync(baseConnectionStringBuilder, MySqlSslMode.VerifyFull);

Console.WriteLine();
Console.WriteLine($"VerifyFull succeeded on this runtime: {verifyFullSucceeded}");
Environment.ExitCode = !onlineChainSucceeded && noCheckChainSucceeded && verifyCaSucceeded ? 0 : 1;

static bool RunChainCheck(string caCertificatePath, string serverCertificatePath, X509RevocationMode revocationMode)
{
	Console.WriteLine($"=== X509Chain ({revocationMode}) ===");

#if NET9_0_OR_GREATER
	using var caCertificate = X509CertificateLoader.LoadCertificateFromFile(caCertificatePath);
	using var serverCertificate = X509CertificateLoader.LoadCertificateFromFile(serverCertificatePath);
#else
	using var caCertificate = new X509Certificate2(caCertificatePath);
	using var serverCertificate = new X509Certificate2(serverCertificatePath);
#endif

	using var chain = new X509Chain();
	chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
	chain.ChainPolicy.CustomTrustStore.Add(caCertificate);
	chain.ChainPolicy.RevocationMode = revocationMode;

	var success = chain.Build(serverCertificate);
	Console.WriteLine($"Build succeeded: {success}");

	for (var index = 0; index < chain.ChainElements.Count; index++)
	{
		var element = chain.ChainElements[index];
		Console.WriteLine($"Element {index}: {element.Certificate.Subject}");
		foreach (var status in element.ChainElementStatus)
			Console.WriteLine($"  {status.Status}: {status.StatusInformation.Trim()}");
	}

	return success;
}

static async Task<bool> TryOpenAsync(MySqlConnectionStringBuilder baseConnectionStringBuilder, MySqlSslMode sslMode)
{
	var connectionStringBuilder = new MySqlConnectionStringBuilder(baseConnectionStringBuilder.ConnectionString)
	{
		SslMode = sslMode,
	};

	Console.WriteLine($"=== {sslMode} ===");
	Console.WriteLine(connectionStringBuilder.ConnectionString);

	await using var connection = new MySqlConnection(connectionStringBuilder.ConnectionString);
	try
	{
		await connection.OpenAsync();
		await using var command = new MySqlCommand("SELECT VERSION();", connection);
		var serverVersion = (string) (await command.ExecuteScalarAsync())!;
		Console.WriteLine($"Connected successfully. Version={serverVersion}");
		return true;
	}
	catch (Exception ex)
	{
		Console.WriteLine("Connection failed:");
		WriteException(ex, 0);
		return false;
	}
}

static void WriteException(Exception ex, int depth)
{
	var indent = new string(' ', depth * 2);
	Console.WriteLine($"{indent}{ex.GetType().FullName}: {ex.Message}");

	if (ex is AuthenticationException authenticationException &&
		authenticationException.InnerException is not null)
	{
		WriteException(authenticationException.InnerException, depth + 1);
		return;
	}

	if (ex.InnerException is not null)
		WriteException(ex.InnerException, depth + 1);
}
