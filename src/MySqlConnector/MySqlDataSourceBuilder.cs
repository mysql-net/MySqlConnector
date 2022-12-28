using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using MySqlConnector.Logging;

namespace MySqlConnector;

/// <summary>
/// <see cref="MySqlDataSourceBuilder"/> provides an API for configuring and creating a <see cref="MySqlDataSource"/>,
/// from which <see cref="MySqlConnection"/> objects can be obtained.
/// </summary>
public sealed class MySqlDataSourceBuilder
{
	/// <summary>
	/// Initializes a new <see cref="MySqlDataSourceBuilder"/> with the specified connection string.
	/// </summary>
	/// <param name="connectionString">The optional connection string to use.</param>
	public MySqlDataSourceBuilder(string? connectionString = null)
	{
		ConnectionStringBuilder = new(connectionString ?? "");
	}

	/// <summary>
	/// Sets the <see cref="ILoggerFactory"/> that will be used for logging.
	/// </summary>
	/// <param name="loggerFactory">The logger factory.</param>
	/// <returns>This builder, so that method calls can be chained.</returns>
	public MySqlDataSourceBuilder UseLoggerFactory(ILoggerFactory? loggerFactory)
	{
		m_loggerFactory = loggerFactory;
		return this;
	}

	/// <summary>
	/// Sets the callback used to provide client certificates for connecting to a server.
	/// </summary>
	/// <param name="callback">The callback that will provide client certificates. The <see cref="X509CertificateCollection"/>
	/// provided to the callback should be filled with the client certificate(s) needed to connect to the server.</param>
	/// <returns>This builder, so that method calls can be chained.</returns>
	public MySqlDataSourceBuilder UseClientCertificatesCallback(Func<X509CertificateCollection, ValueTask>? callback)
	{
		m_clientCertificatesCallback = callback;
		return this;
	}

	/// <summary>
	/// Sets the callback used to verify that the server's certificate is valid.
	/// </summary>
	/// <param name="callback">The callback used to verify that the server's certificate is valid.</param>
	/// <returns>This builder, so that method calls can be chained.</returns>
	/// <remarks><see cref="MySqlConnectionStringBuilder.SslMode"/> must be set to <see cref="MySqlSslMode.Preferred"/>
	/// or <see cref="MySqlSslMode.Required"/> in order for this delegate to be invoked. See the documentation for
	/// <see cref="RemoteCertificateValidationCallback"/> for more information on the values passed to this delegate.</remarks>
	public MySqlDataSourceBuilder UseRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
	{
		m_remoteCertificateValidationCallback = callback;
		return this;
	}

	/// <summary>
	/// Builds a <see cref="MySqlDataSource"/> which is ready for use.
	/// </summary>
	/// <returns>A new <see cref="MySqlDataSource"/> with the settings configured through this <see cref="MySqlDataSourceBuilder"/>.</returns>
	public MySqlDataSource Build()
	{
		var loggingConfiguration = m_loggerFactory is null ? MySqlConnectorLoggingConfiguration.NullConfiguration : new(m_loggerFactory);
		return new(ConnectionStringBuilder.ConnectionString,
			loggingConfiguration,
			m_clientCertificatesCallback,
			m_remoteCertificateValidationCallback
			);
	}

	/// <summary>
	/// A <see cref="MySqlConnectionStringBuilder"/> that can be used to configure the connection string on this <see cref="MySqlDataSourceBuilder"/>.
	/// </summary>
	public MySqlConnectionStringBuilder ConnectionStringBuilder { get; }

	private ILoggerFactory? m_loggerFactory;
	private Func<X509CertificateCollection, ValueTask>? m_clientCertificatesCallback;
	private RemoteCertificateValidationCallback? m_remoteCertificateValidationCallback;
}
