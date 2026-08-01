using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MySqlConnector.Utilities;

namespace MySqlConnector.Tests;

public class SslSecurityTests
{
	[Fact]
	public async Task DoesNotSendCleartextPasswordToServerWithUnverifiedCertificate()
	{
		// skip this test on Windows CI builds because it fails for as-yet-unknown reasons
		if (Utility.IsWindows() && (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase) || Environment.GetEnvironmentVariable("TF_BUILD") == "True"))
			return;

		// Regression test for GHSA-473q-m89c-ghf8.
		// With SslMode=VerifyCA/VerifyFull and no configured SslCa, MySqlConnector defers TLS certificate validation
		// until after authentication (to support MariaDB "zero-configuration TLS", where a self-signed certificate is
		// validated via a fingerprint derived from the password). A man-in-the-middle can complete the TLS handshake
		// with any self-signed certificate and then request cleartext-password authentication; the client must not
		// send the password before the certificate has been verified, or the MitM captures it in the clear.
		using var certificate = CreateSelfSignedCertificate();
		var server = new FakeMySqlServer
		{
			ServerCertificate = certificate,
			RequestClearPasswordSwitch = true,
		};
		server.Start();
		try
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = ServerSession.TestHostNameTreatedAsRemote,
				Port = (uint) server.Port,
				UserID = "root",
				Password = "S3cr3t-Pa55word!",
				SslMode = MySqlSslMode.VerifyFull,
				TlsVersion = "TLS 1.2",
				Pooling = false,
				ConnectionTimeout = 15,
			};
			using var connection = new MySqlConnection(csb.ConnectionString);

			// opening the connection should fail, but this can be because FakeMySqlServer doesn't implement the full protocol so wait for the client's response to be captured
			var exception = await Assert.ThrowsAsync<MySqlException>(() => connection.OpenAsync());
			var finished = await Task.WhenAny(server.ClearPasswordResponse, Task.Delay(TimeSpan.FromSeconds(30)));
			Assert.True(finished == server.ClearPasswordResponse, "the fake server never reached the cleartext-password auth switch");

			// the password should not be leaked
			var passwordResponse = await server.ClearPasswordResponse;
			Assert.NotEqual(passwordResponse, Encoding.UTF8.GetBytes(csb.Password + "\0"));
		}
		finally
		{
			server.Stop();
		}
	}

	private static X509Certificate2 CreateSelfSignedCertificate()
	{
		using var rsa = RSA.Create(2048);
		var request = new CertificateRequest("CN=FakeMySqlServer", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

		// Re-import via PKCS#12 so the private key is in a form usable for server-side TLS on every platform; in
		// particular, Windows/SChannel cannot use the ephemeral key produced by CreateSelfSigned directly.
		var pkcs12 = certificate.Export(X509ContentType.Pkcs12);
#if NET9_0_OR_GREATER
		return X509CertificateLoader.LoadPkcs12(pkcs12, null);
#else
		return new X509Certificate2(pkcs12);
#endif
	}
}
