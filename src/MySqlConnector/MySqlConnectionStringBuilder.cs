using System.Collections;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MySqlConnector;

#pragma warning disable CA1010 // Generic interface should also be implemented

/// <summary>
/// <see cref="MySqlConnectionStringBuilder"/> allows you to construct a MySQL connection string by setting properties on the builder then reading the <see cref="DbConnectionStringBuilder.ConnectionString"/> property.
/// </summary>
/// <remarks>See <a href="https://mysqlconnector.net/connection-options/">Connection String Options</a> for more documentation on the options.</remarks>
#if NET6_0_OR_GREATER && !NET10_0_OR_GREATER
[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2113:ReflectionToRequiresUnreferencedCode", Justification = "Suppressing the same warnings as suppressed in the base DbConnectionStringBuilder.")]
#endif
public sealed class MySqlConnectionStringBuilder : DbConnectionStringBuilder
{
	/// <summary>
	/// Initializes a new <see cref="MySqlConnectionStringBuilder"/>.
	/// </summary>
	public MySqlConnectionStringBuilder()
	{
	}

	/// <summary>
	/// Initializes a new <see cref="MySqlConnectionStringBuilder"/> with properties set from the specified connection string.
	/// </summary>
	/// <param name="connectionString">The connection string to use to set property values on this object.</param>
	public MySqlConnectionStringBuilder(string connectionString)
	{
		ConnectionString = connectionString;
	}

	// Connection Options

	/// <summary>
	/// <para>The host name or network address of the MySQL Server to which to connect. Multiple hosts can be specified in a comma-delimited list.</para>
	/// <para>On Unix-like systems, this can be a fully qualified path to a MySQL socket file, which will cause a Unix socket to be used instead of a TCP/IP socket. Only a single socket name can be specified.</para>
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("")]
	[Description("The host name or network address of the MySQL Server to which to connect.")]
	[DisplayName("Server")]
	public string Server
	{
		get => MySqlConnectionStringOption.Server.GetValue(this);
		set => MySqlConnectionStringOption.Server.SetValue(this, value);
	}

	/// <summary>
	/// The TCP port on which MySQL Server is listening for connections.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(3306u)]
	[Description("The TCP port on which MySQL Server is listening for connections.")]
	[DisplayName("Port")]
	public uint Port
	{
		get => MySqlConnectionStringOption.Port.GetValue(this);
		set => MySqlConnectionStringOption.Port.SetValue(this, value);
	}

	/// <summary>
	/// The MySQL user ID.
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("")]
	[Description("The MySQL user ID.")]
	[DisplayName("User ID")]
	public string UserID
	{
		get => MySqlConnectionStringOption.UserID.GetValue(this);
		set => MySqlConnectionStringOption.UserID.SetValue(this, value);
	}

	/// <summary>
	/// The password for the MySQL user.
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("")]
	[Description("The password for the MySQL user.")]
	[DisplayName("Password")]
	public string Password
	{
		get => MySqlConnectionStringOption.Password.GetValue(this);
		set => MySqlConnectionStringOption.Password.SetValue(this, value);
	}

	/// <summary>
	/// (Optional) The case-sensitive name of the initial database to use. This may be required if the MySQL user account only has access rights to particular databases on the server.
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("The case-sensitive name of the initial database to use.")]
	[Description("The case-sensitive name of the initial database to use")]
	[DisplayName("Database")]
	public string Database
	{
		get => MySqlConnectionStringOption.Database.GetValue(this);
		set => MySqlConnectionStringOption.Database.SetValue(this, value);
	}

	/// <summary>
	/// Specifies how load is distributed across backend servers.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(MySqlLoadBalance.RoundRobin)]
	[Description("Specifies how load is distributed across backend servers.")]
	[DisplayName("Load Balance")]
	public MySqlLoadBalance LoadBalance
	{
		get => MySqlConnectionStringOption.LoadBalance.GetValue(this);
		set => MySqlConnectionStringOption.LoadBalance.SetValue(this, value);
	}

	/// <summary>
	/// The protocol to use to connect to the MySQL Server.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(MySqlConnectionProtocol.Socket)]
	[Description("The protocol to use to connect to the MySQL Server.")]
	[DisplayName("Connection Protocol")]
	public MySqlConnectionProtocol ConnectionProtocol
	{
		get => MySqlConnectionStringOption.ConnectionProtocol.GetValue(this);
		set => MySqlConnectionStringOption.ConnectionProtocol.SetValue(this, value);
	}

	/// <summary>
	/// The name of the Windows named pipe to use to connect to the server. You must also set <see cref="ConnectionProtocol"/> to <see cref="MySqlConnectionProtocol.NamedPipe"/> to used named pipes.
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("MYSQL")]
	[Description("The name of the Windows named pipe to use to connect to the server.")]
	[DisplayName("Pipe Name")]
	public string PipeName
	{
		get => MySqlConnectionStringOption.PipeName.GetValue(this);
		set => MySqlConnectionStringOption.PipeName.SetValue(this, value);
	}

	// SSL/TLS Options

	/// <summary>
	/// Whether to use SSL/TLS when connecting to the MySQL server.
	/// </summary>
	[Category("TLS")]
	[DefaultValue(MySqlSslMode.Preferred)]
	[Description("Whether to use SSL/TLS when connecting to the MySQL server.")]
	[DisplayName("SSL Mode")]
	public MySqlSslMode SslMode
	{
		get => MySqlConnectionStringOption.SslMode.GetValue(this);
		set => MySqlConnectionStringOption.SslMode.SetValue(this, value);
	}

	/// <summary>
	/// The path to a certificate file in PKCS #12 (.pfx) format containing a bundled Certificate and Private Key used for mutual authentication.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The path to a certificate file in PKCS #12 (.pfx) format containing a bundled Certificate and Private Key used for mutual authentication.")]
	[DisplayName("Certificate File")]
	public string CertificateFile
	{
		get => MySqlConnectionStringOption.CertificateFile.GetValue(this);
		set => MySqlConnectionStringOption.CertificateFile.SetValue(this, value);
	}

	/// <summary>
	/// The password for the certificate specified using the <see cref="CertificateFile"/> option. Not required if the certificate file is not password protected.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The password for the certificate specified using the Certificate File option.")]
	[DisplayName("Certificate Password")]
	public string CertificatePassword
	{
		get => MySqlConnectionStringOption.CertificatePassword.GetValue(this);
		set => MySqlConnectionStringOption.CertificatePassword.SetValue(this, value);
	}

	/// <summary>
	/// Uses a certificate from the specified Certificate Store on the machine. The default value of <see cref="MySqlCertificateStoreLocation.None"/> means the certificate store is not used; a value of <see cref="MySqlCertificateStoreLocation.CurrentUser"/> or <see cref="MySqlCertificateStoreLocation.LocalMachine"/> uses the specified store.
	/// </summary>
	[Category("TLS")]
	[DefaultValue(MySqlCertificateStoreLocation.None)]
	[Description("Uses a certificate from the specified Certificate Store on the machine.")]
	[DisplayName("Certificate Store Location")]
	public MySqlCertificateStoreLocation CertificateStoreLocation
	{
		get => MySqlConnectionStringOption.CertificateStoreLocation.GetValue(this);
		set => MySqlConnectionStringOption.CertificateStoreLocation.SetValue(this, value);
	}

	/// <summary>
	/// Specifies which certificate should be used from the Certificate Store specified in <see cref="CertificateStoreLocation"/>. This option must be used to indicate which certificate in the store should be used for authentication.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DisplayName("Certificate Thumbprint")]
	[DefaultValue("")]
	[Description("Specifies which certificate should be used from the certificate store specified in Certificate Store Location")]
	public string CertificateThumbprint
	{
		get => MySqlConnectionStringOption.CertificateThumbprint.GetValue(this);
		set => MySqlConnectionStringOption.CertificateThumbprint.SetValue(this, value);
	}

	/// <summary>
	/// The path to the client’s SSL certificate file in PEM format. <see cref="SslKey"/> must also be specified, and <see cref="CertificateFile"/> should not be.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The path to the client’s SSL certificate file in PEM format.")]
	[DisplayName("SSL Cert")]
	public string SslCert
	{
		get => MySqlConnectionStringOption.SslCert.GetValue(this);
		set => MySqlConnectionStringOption.SslCert.SetValue(this, value);
	}

	/// <summary>
	/// The path to the client’s SSL private key in PEM format. <see cref="SslCert"/> must also be specified, and <see cref="CertificateFile"/> should not be.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The path to the client’s SSL private key in PEM format.")]
	[DisplayName("SSL Key")]
	public string SslKey
	{
		get => MySqlConnectionStringOption.SslKey.GetValue(this);
		set => MySqlConnectionStringOption.SslKey.SetValue(this, value);
	}

	/// <summary>
	/// Use <see cref="SslCa"/> instead.
	/// </summary>
	[AllowNull]
	[Browsable(false)]
	[Category("Obsolete")]
	[DisplayName("CA Certificate File")]
	[Obsolete("Use SslCa instead.")]
	public string CACertificateFile
	{
		get => MySqlConnectionStringOption.SslCa.GetValue(this);
		set => MySqlConnectionStringOption.SslCa.SetValue(this, value);
	}

	/// <summary>
	/// The path to a CA certificate file in a PEM Encoded (.pem) format. This should be used with a value for the <see cref="SslMode"/> property of <see cref="MySqlSslMode.VerifyCA"/> or <see cref="MySqlSslMode.VerifyFull"/> to enable verification of a CA certificate that is not trusted by the operating system’s certificate store.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The path to a CA certificate file in a PEM Encoded (.pem) format.")]
	[DisplayName("SSL CA")]
	public string SslCa
	{
		get => MySqlConnectionStringOption.SslCa.GetValue(this);
		set => MySqlConnectionStringOption.SslCa.SetValue(this, value);
	}

	/// <summary>
	/// The TLS versions which may be used during TLS negotiation, or empty to use OS defaults.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DisplayName("TLS Version")]
	[DefaultValue("")]
	[Description("The TLS versions which may be used during TLS negotiation.")]
	public string TlsVersion
	{
		get => MySqlConnectionStringOption.TlsVersion.GetValue(this);
		set => MySqlConnectionStringOption.TlsVersion.SetValue(this, value);
	}

	/// <summary>
	/// The TLS cipher suites which may be used during TLS negotiation. The default value (the empty string) allows the OS to determine the TLS cipher suites to use; this is the recommended setting.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The TLS cipher suites which may be used during TLS negotiation.")]
	[DisplayName("TLS Cipher Suites")]
	public string TlsCipherSuites
	{
		get => MySqlConnectionStringOption.TlsCipherSuites.GetValue(this);
		set => MySqlConnectionStringOption.TlsCipherSuites.SetValue(this, value);
	}

	// Connection Pooling Options

	/// <summary>
	/// Enables connection pooling.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(true)]
	[Description("Enables connection pooling.")]
	[DisplayName("Pooling")]
	public bool Pooling
	{
		get => MySqlConnectionStringOption.Pooling.GetValue(this);
		set => MySqlConnectionStringOption.Pooling.SetValue(this, value);
	}

	/// <summary>
	/// The maximum lifetime (in seconds) for any connection, or <c>0</c> for no lifetime limit.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(0u)]
	[Description("The maximum lifetime (in seconds) for any connection, or 0 for no lifetime limit.")]
	[DisplayName("Connection Lifetime")]
	public uint ConnectionLifeTime
	{
		get => MySqlConnectionStringOption.ConnectionLifeTime.GetValue(this);
		set => MySqlConnectionStringOption.ConnectionLifeTime.SetValue(this, value);
	}

	/// <summary>
	/// Whether connections are reset when being retrieved from the pool.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(true)]
	[Description("Whether connections are reset when being retrieved from the pool.")]
	[DisplayName("Connection Reset")]
	public bool ConnectionReset
	{
		get => MySqlConnectionStringOption.ConnectionReset.GetValue(this);
		set => MySqlConnectionStringOption.ConnectionReset.SetValue(this, value);
	}

	/// <summary>
	/// This option is no longer supported.
	/// </summary>
	[Category("Obsolete")]
	[DefaultValue(true)]
	[DisplayName("Defer Connection Reset")]
	[Obsolete("This option is no longer supported in MySqlConnector >= 1.4.0.")]
	public bool DeferConnectionReset
	{
		get => MySqlConnectionStringOption.DeferConnectionReset.GetValue(this);
		set => MySqlConnectionStringOption.DeferConnectionReset.SetValue(this, value);
	}

	/// <summary>
	/// This option is no longer supported.
	/// </summary>
	[Category("Obsolete")]
	[DefaultValue(0u)]
	[DisplayName("Connection Idle Ping Time")]
	[Obsolete("This option is no longer supported in MySqlConnector >= 1.4.0.")]
	public uint ConnectionIdlePingTime
	{
		get => MySqlConnectionStringOption.ConnectionIdlePingTime.GetValue(this);
		set => MySqlConnectionStringOption.ConnectionIdlePingTime.SetValue(this, value);
	}

	/// <summary>
	/// The amount of time (in seconds) that a connection can remain idle in the pool.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(180u)]
	[Description("The amount of time (in seconds) that a connection can remain idle in the pool.")]
	[DisplayName("Connection Idle Timeout")]
	public uint ConnectionIdleTimeout
	{
		get => MySqlConnectionStringOption.ConnectionIdleTimeout.GetValue(this);
		set => MySqlConnectionStringOption.ConnectionIdleTimeout.SetValue(this, value);
	}

	/// <summary>
	/// The minimum number of connections to leave in the pool if <see cref="ConnectionIdleTimeout"/> is reached.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(0u)]
	[Description("The minimum number of connections to leave in the pool if Connection Idle Timeout is reached.")]
	[DisplayName("Minimum Pool Size")]
	public uint MinimumPoolSize
	{
		get => MySqlConnectionStringOption.MinimumPoolSize.GetValue(this);
		set => MySqlConnectionStringOption.MinimumPoolSize.SetValue(this, value);
	}

	/// <summary>
	/// The maximum number of connections allowed in the pool.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(100u)]
	[Description("The maximum number of connections allowed in the pool.")]
	[DisplayName("Maximum Pool Size")]
	public uint MaximumPoolSize
	{
		get => MySqlConnectionStringOption.MaximumPoolSize.GetValue(this);
		set => MySqlConnectionStringOption.MaximumPoolSize.SetValue(this, value);
	}

	/// <summary>
	/// The number of seconds between checks for DNS changes, or 0 to disable periodic checks.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(0u)]
	[Description("The number of seconds between checks for DNS changes.")]
	[DisplayName("DNS Check Interval")]
	public uint DnsCheckInterval
	{
		get => MySqlConnectionStringOption.DnsCheckInterval.GetValue(this);
		set => MySqlConnectionStringOption.DnsCheckInterval.SetValue(this, value);
	}

	// Other Options

	/// <summary>
	/// Allows the <c>LOAD DATA LOCAL</c> command to request files from the client.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Allows the LOAD DATA LOCAL command to request files from the client.")]
	[DisplayName("Allow Load Local Infile")]
	public bool AllowLoadLocalInfile
	{
		get => MySqlConnectionStringOption.AllowLoadLocalInfile.GetValue(this);
		set => MySqlConnectionStringOption.AllowLoadLocalInfile.SetValue(this, value);
	}

	/// <summary>
	/// Allows the client to automatically request the RSA public key from the server.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Allows the client to automatically request the RSA public key from the server.")]
	[DisplayName("Allow Public Key Retrieval")]
	public bool AllowPublicKeyRetrieval
	{
		get => MySqlConnectionStringOption.AllowPublicKeyRetrieval.GetValue(this);
		set => MySqlConnectionStringOption.AllowPublicKeyRetrieval.SetValue(this, value);
	}

	/// <summary>
	/// Allows user-defined variables (prefixed with <c>@</c>) to be used in SQL statements.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Allows user-defined variables (prefixed with @) to be used in SQL statements.")]
	[DisplayName("Allow User Variables")]
	public bool AllowUserVariables
	{
		get => MySqlConnectionStringOption.AllowUserVariables.GetValue(this);
		set => MySqlConnectionStringOption.AllowUserVariables.SetValue(this, value);
	}

	/// <summary>
	/// Returns <c>DATETIME</c> fields as <see cref="MySqlDateTime"/> objects instead of <see cref="DateTime"/> objects.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Returns DATETIME fields as MySqlDateTime objects instead of DateTime objects.")]
	[DisplayName("Allow Zero DateTime")]
	public bool AllowZeroDateTime
	{
		get => MySqlConnectionStringOption.AllowZeroDateTime.GetValue(this);
		set => MySqlConnectionStringOption.AllowZeroDateTime.SetValue(this, value);
	}

	/// <summary>
	/// Sets the <c>program_name</c> connection attribute passed to MySQL Server.
	/// </summary>
	[AllowNull]
	[Category("Other")]
	[DefaultValue("")]
	[Description("Sets the program_name connection attribute passed to MySQL Server.")]
	[DisplayName("Application Name")]
	public string ApplicationName
	{
		get => MySqlConnectionStringOption.ApplicationName.GetValue(this);
		set => MySqlConnectionStringOption.ApplicationName.SetValue(this, value);
	}

	/// <summary>
	/// Automatically enlists this connection in any active <see cref="System.Transactions.TransactionScope"/>.
	/// </summary>
	[Category("Other")]
	[DefaultValue(true)]
	[Description("Automatically enlists this connection in any active TransactionScope.")]
	[DisplayName("Auto Enlist")]
	public bool AutoEnlist
	{
		get => MySqlConnectionStringOption.AutoEnlist.GetValue(this);
		set => MySqlConnectionStringOption.AutoEnlist.SetValue(this, value);
	}

	/// <summary>
	/// The length of time (in seconds) to wait for a query to be canceled when <see cref="MySqlCommand.CommandTimeout"/> expires, or zero for no timeout.
	/// </summary>
	[Category("Other")]
	[DefaultValue(2)]
	[Description("The length of time (in seconds) to wait for a query to be canceled when MySqlCommand.CommandTimeout expires, or zero for no timeout.")]
	[DisplayName("Cancellation Timeout")]
	public int CancellationTimeout
	{
		get => MySqlConnectionStringOption.CancellationTimeout.GetValue(this);
		set => MySqlConnectionStringOption.CancellationTimeout.SetValue(this, value);
	}

	/// <summary>
	/// Supported for backwards compatibility; MySqlConnector always uses <c>utf8mb4</c>.
	/// </summary>
	[AllowNull]
	[Category("Obsolete")]
	[DefaultValue("")]
	[DisplayName("Character Set")]
	public string CharacterSet
	{
		get => MySqlConnectionStringOption.CharacterSet.GetValue(this);
		set => MySqlConnectionStringOption.CharacterSet.SetValue(this, value);
	}

	/// <summary>
	/// The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.
	/// The default value is 15.
	/// </summary>
	[Category("Connection")]
	[Description("The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.")]
	[DefaultValue(15u)]
	[DisplayName("Connection Timeout")]
	public uint ConnectionTimeout
	{
		get => MySqlConnectionStringOption.ConnectionTimeout.GetValue(this);
		set => MySqlConnectionStringOption.ConnectionTimeout.SetValue(this, value);
	}

	/// <summary>
	/// Whether invalid <c>DATETIME</c> fields should be converted to <see cref="DateTime.MinValue"/>.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Whether invalid DATETIME fields should be converted to DateTime.MinValue.")]
	[DisplayName("Convert Zero DateTime")]
	public bool ConvertZeroDateTime
	{
		get => MySqlConnectionStringOption.ConvertZeroDateTime.GetValue(this);
		set => MySqlConnectionStringOption.ConvertZeroDateTime.SetValue(this, value);
	}

	/// <summary>
	/// The <see cref="DateTimeKind"/> to use when deserializing <c>DATETIME</c> values.
	/// </summary>
	[Category("Other")]
	[DefaultValue(MySqlDateTimeKind.Unspecified)]
	[Description("The DateTimeKind to use when deserializing DATETIME values.")]
	[DisplayName("DateTime Kind")]
	public MySqlDateTimeKind DateTimeKind
	{
		get => MySqlConnectionStringOption.DateTimeKind.GetValue(this);
		set => MySqlConnectionStringOption.DateTimeKind.SetValue(this, value);
	}

	/// <summary>
	/// The length of time (in seconds) each command can execute before the query is cancelled on the server, or zero to disable timeouts.
	/// </summary>
	[Category("Other")]
	[DefaultValue(30u)]
	[Description("The length of time (in seconds) each command can execute before the query is cancelled on the server, or zero to disable timeouts.")]
	[DisplayName("Default Command Timeout")]
	public uint DefaultCommandTimeout
	{
		get => MySqlConnectionStringOption.DefaultCommandTimeout.GetValue(this);
		set => MySqlConnectionStringOption.DefaultCommandTimeout.SetValue(this, value);
	}

	/// <summary>
	/// Forces all async methods to execute synchronously. This can be useful for debugging.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Forces all async methods to execute synchronously.")]
	[DisplayName("Force Synchronous")]
	public bool ForceSynchronous
	{
		get => MySqlConnectionStringOption.ForceSynchronous.GetValue(this);
		set => MySqlConnectionStringOption.ForceSynchronous.SetValue(this, value);
	}

	/// <summary>
	/// Determines which column type (if any) should be read as a <see cref="Guid"/>.
	/// </summary>
	[Category("Other")]
	[DefaultValue(MySqlGuidFormat.Default)]
	[Description("Determines which column type (if any) should be read as a Guid.")]
	[DisplayName("GUID Format")]
	public MySqlGuidFormat GuidFormat
	{
		get => MySqlConnectionStringOption.GuidFormat.GetValue(this);
		set => MySqlConnectionStringOption.GuidFormat.SetValue(this, value);
	}

	/// <summary>
	/// Does not check the <see cref="MySqlCommand.Transaction"/> property for validity when executing a command.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Does not check the MySqlCommand.Transaction property for validity when executing a command.")]
	[DisplayName("Ignore Command Transaction")]
	public bool IgnoreCommandTransaction
	{
		get => MySqlConnectionStringOption.IgnoreCommandTransaction.GetValue(this);
		set => MySqlConnectionStringOption.IgnoreCommandTransaction.SetValue(this, value);
	}

	/// <summary>
	/// Ignores calls to <see cref="MySqlCommand.Prepare"/> and <c>PrepareAsync</c>.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Ignores calls to MySqlCommand.Prepare and PrepareAsync.")]
	[DisplayName("Ignore Prepare")]
	public bool IgnorePrepare
	{
		get => MySqlConnectionStringOption.IgnorePrepare.GetValue(this);
		set => MySqlConnectionStringOption.IgnorePrepare.SetValue(this, value);
	}

	/// <summary>
	/// Instructs the MySQL server that this is an interactive session.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(false)]
	[Description("Instructs the MySQL server that this is an interactive session.")]
	[DisplayName("Interactive Session")]
	public bool InteractiveSession
	{
		get => MySqlConnectionStringOption.InteractiveSession.GetValue(this);
		set => MySqlConnectionStringOption.InteractiveSession.SetValue(this, value);
	}

	/// <summary>
	/// TCP Keepalive idle time (in seconds), or 0 to use OS defaults.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(0u)]
	[Description("TCP Keepalive idle time (in seconds), or 0 to use OS defaults.")]
	[DisplayName("Keep Alive")]
	public uint Keepalive
	{
		get => MySqlConnectionStringOption.Keepalive.GetValue(this);
		set => MySqlConnectionStringOption.Keepalive.SetValue(this, value);
	}

	/// <summary>
	/// Doesn't escape backslashes in string parameters. For use with the <c>NO_BACKSLASH_ESCAPES</c> MySQL server mode.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Doesn't escape backslashes in string parameters. For use with the NO_BACKSLASH_ESCAPES MySQL server mode.")]
	[DisplayName("No Backslash Escapes")]
	public bool NoBackslashEscapes
	{
		get => MySqlConnectionStringOption.NoBackslashEscapes.GetValue(this);
		set => MySqlConnectionStringOption.NoBackslashEscapes.SetValue(this, value);
	}

	/// <summary>
	/// Use the <see cref="GuidFormat"/> property instead.
	/// </summary>
	[Category("Obsolete")]
	[DisplayName("Old Guids")]
	[DefaultValue(false)]
	public bool OldGuids
	{
		get => MySqlConnectionStringOption.OldGuids.GetValue(this);
		set => MySqlConnectionStringOption.OldGuids.SetValue(this, value);
	}

	/// <summary>
	/// If true, preserves security-sensitive information in the connection string retrieved from any open <see cref="MySqlConnection"/>.
	/// </summary>
	[Category("Other")]
	[DisplayName("Persist Security Info")]
	[DefaultValue(false)]
	[Description("Preserves security-sensitive information in the connection string retrieved from any open MySqlConnection.")]
	public bool PersistSecurityInfo
	{
		get => MySqlConnectionStringOption.PersistSecurityInfo.GetValue(this);
		set => MySqlConnectionStringOption.PersistSecurityInfo.SetValue(this, value);
	}

	/// <summary>
	/// Enables query pipelining.
	/// </summary>
	[Category("Other")]
	[DefaultValue(true)]
	[Description("Enables query pipelining.")]
	[DisplayName("Pipelining")]
	public bool Pipelining
	{
		get => MySqlConnectionStringOption.Pipelining.GetValue(this);
		set => MySqlConnectionStringOption.Pipelining.SetValue(this, value);
	}

	/// <summary>
	/// Whether to use server redirection.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(MySqlServerRedirectionMode.Disabled)]
	[Description("Whether to use server redirection.")]
	[DisplayName("Server Redirection Mode")]
	public MySqlServerRedirectionMode ServerRedirectionMode
	{
		get => MySqlConnectionStringOption.ServerRedirectionMode.GetValue(this);
		set => MySqlConnectionStringOption.ServerRedirectionMode.SetValue(this, value);
	}

	/// <summary>
	/// The path to a file containing the server's RSA public key.
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DisplayName("Server RSA Public Key File")]
	[DefaultValue("")]
	[Description("The path to a file containing the server's RSA public key.")]
	public string ServerRsaPublicKeyFile
	{
		get => MySqlConnectionStringOption.ServerRsaPublicKeyFile.GetValue(this);
		set => MySqlConnectionStringOption.ServerRsaPublicKeyFile.SetValue(this, value);
	}

	/// <summary>
	/// The server’s Service Principal Name (for <c>auth_gssapi_client</c> authentication).
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("")]
	[Description("The server’s Service Principal Name (for auth_gssapi_client authentication).")]
	[DisplayName("Server SPN")]
	public string ServerSPN
	{
		get => MySqlConnectionStringOption.ServerSPN.GetValue(this);
		set => MySqlConnectionStringOption.ServerSPN.SetValue(this, value);
	}

	/// <summary>
	/// Returns <c>TINYINT(1)</c> fields as <see cref="bool"/> values.
	/// </summary>
	[Category("Other")]
	[DisplayName("Treat Tiny As Boolean")]
	[DefaultValue(true)]
	[Description("Returns TINYINT(1) fields as Boolean values.")]
	public bool TreatTinyAsBoolean
	{
		get => MySqlConnectionStringOption.TreatTinyAsBoolean.GetValue(this);
		set => MySqlConnectionStringOption.TreatTinyAsBoolean.SetValue(this, value);
	}

	/// <summary>
	/// Report changed rows instead of found rows.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Report changed rows instead of found rows.")]
	[DisplayName("Use Affected Rows")]
	public bool UseAffectedRows
	{
		get => MySqlConnectionStringOption.UseAffectedRows.GetValue(this);
		set => MySqlConnectionStringOption.UseAffectedRows.SetValue(this, value);
	}

	/// <summary>
	/// Compress packets sent to and from the server.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Compress packets sent to and from the server.")]
	[DisplayName("Use Compression")]
	public bool UseCompression
	{
		get => MySqlConnectionStringOption.UseCompression.GetValue(this);
		set => MySqlConnectionStringOption.UseCompression.SetValue(this, value);
	}

	/// <summary>
	/// Use XA transactions to implement <see cref="System.Transactions.TransactionScope"/> distributed transactions.
	/// </summary>
	[Category("Other")]
	[DefaultValue(true)]
	[Description("Use XA transactions to implement System.Transactions distributed transactions.")]
	[DisplayName("Use XA Transactions")]
	public bool UseXaTransactions
	{
		get => MySqlConnectionStringOption.UseXaTransactions.GetValue(this);
		set => MySqlConnectionStringOption.UseXaTransactions.SetValue(this, value);
	}

	// Other Methods

	/// <summary>
	/// Returns an <see cref="ICollection"/> that contains the keys in the <see cref="MySqlConnectionStringBuilder"/>.
	/// </summary>
	public override ICollection Keys => base.Keys.Cast<string>().OrderBy(static x => MySqlConnectionStringOption.OptionNames.IndexOf(x)).ToList();

	/// <summary>
	/// Whether this <see cref="MySqlConnectionStringBuilder"/> contains a set option with the specified name.
	/// </summary>
	/// <param name="keyword">The option name.</param>
	/// <returns><c>true</c> if an option with that name is set; otherwise, <c>false</c>.</returns>
	public override bool ContainsKey(string keyword) =>
		MySqlConnectionStringOption.TryGetOptionForKey(keyword) is { } option && base.ContainsKey(option.Key);

	/// <summary>
	/// Removes the option with the specified name.
	/// </summary>
	/// <param name="keyword">The option name.</param>
	public override bool Remove(string keyword) =>
		MySqlConnectionStringOption.TryGetOptionForKey(keyword) is { } option && base.Remove(option.Key);

	/// <summary>
	/// Retrieves an option value by name.
	/// </summary>
	/// <param name="key">The option name.</param>
	/// <returns>That option's value, if set.</returns>
	[AllowNull]
	public override object this[string key]
	{
		get => MySqlConnectionStringOption.GetOptionForKey(key).GetObject(this);
		set
		{
			var option = MySqlConnectionStringOption.GetOptionForKey(key);
			if (value is null)
				base[option.Key] = null;
			else
				option.SetObject(this, value);
		}
	}

	internal void DoSetValue(string key, object? value) => base[key] = value;

	internal string GetConnectionString(bool includePassword)
	{
		var connectionString = ConnectionString;
		if (includePassword)
			return connectionString;

		if (m_cachedConnectionString != connectionString)
		{
			var csb = new MySqlConnectionStringBuilder(connectionString);
			foreach (string? key in Keys)
			{
				foreach (var passwordKey in MySqlConnectionStringOption.Password.Keys)
				{
					if (string.Equals(key, passwordKey, StringComparison.OrdinalIgnoreCase))
						csb.Remove(key!);
				}
			}
			m_cachedConnectionStringWithoutPassword = csb.ConnectionString;
			m_cachedConnectionString = connectionString;
		}

		return m_cachedConnectionStringWithoutPassword!;
	}

	/// <summary>
	/// Fills in <paramref name="propertyDescriptors"/> with information about the available properties on this object.
	/// </summary>
	/// <param name="propertyDescriptors">The collection of <see cref="PropertyDescriptor"/> objects to populate.</param>
#if NET6_0_OR_GREATER
	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
		Justification = "Suppressing the same warnings as suppressed in the base DbConnectionStringBuilder. See https://github.com/mysql-net/MySqlConnector/issues/1607")]
#endif
	protected override void GetProperties(Hashtable propertyDescriptors)
	{
		base.GetProperties(propertyDescriptors);

		// only report properties with a [Category] attribute that are not [Obsolete]
		var propertiesToRemove = propertyDescriptors.Values
			.Cast<PropertyDescriptor>()
			.Where(static x => !x.Attributes.OfType<CategoryAttribute>().Any() || x.Attributes.OfType<ObsoleteAttribute>().Any())
			.ToList();
		foreach (var property in propertiesToRemove)
			propertyDescriptors.Remove(property.DisplayName);
	}

	private string? m_cachedConnectionString;
	private string? m_cachedConnectionStringWithoutPassword;
}

internal abstract partial class MySqlConnectionStringOption
{
	public static List<string> OptionNames { get; } = [];

	// Connection Options
	public static readonly MySqlConnectionStringReferenceOption<string> Server;
	public static readonly MySqlConnectionStringValueOption<uint> Port;
	public static readonly MySqlConnectionStringReferenceOption<string> UserID;
	public static readonly MySqlConnectionStringReferenceOption<string> Password;
	public static readonly MySqlConnectionStringReferenceOption<string> Database;
	public static readonly MySqlConnectionStringValueOption<MySqlLoadBalance> LoadBalance;
	public static readonly MySqlConnectionStringValueOption<MySqlConnectionProtocol> ConnectionProtocol;
	public static readonly MySqlConnectionStringReferenceOption<string> PipeName;

	// SSL/TLS Options
	public static readonly MySqlConnectionStringValueOption<MySqlSslMode> SslMode;
	public static readonly MySqlConnectionStringReferenceOption<string> CertificateFile;
	public static readonly MySqlConnectionStringReferenceOption<string> CertificatePassword;
	public static readonly MySqlConnectionStringValueOption<MySqlCertificateStoreLocation> CertificateStoreLocation;
	public static readonly MySqlConnectionStringReferenceOption<string> CertificateThumbprint;
	public static readonly MySqlConnectionStringReferenceOption<string> SslCert;
	public static readonly MySqlConnectionStringReferenceOption<string> SslKey;
	public static readonly MySqlConnectionStringReferenceOption<string> SslCa;
	public static readonly MySqlConnectionStringReferenceOption<string> TlsVersion;
	public static readonly MySqlConnectionStringReferenceOption<string> TlsCipherSuites;

	// Connection Pooling Options
	public static readonly MySqlConnectionStringValueOption<bool> Pooling;
	public static readonly MySqlConnectionStringValueOption<uint> ConnectionLifeTime;
	public static readonly MySqlConnectionStringValueOption<bool> ConnectionReset;
	public static readonly MySqlConnectionStringValueOption<bool> DeferConnectionReset;
	public static readonly MySqlConnectionStringValueOption<uint> ConnectionIdlePingTime;
	public static readonly MySqlConnectionStringValueOption<uint> ConnectionIdleTimeout;
	public static readonly MySqlConnectionStringValueOption<uint> MinimumPoolSize;
	public static readonly MySqlConnectionStringValueOption<uint> MaximumPoolSize;
	public static readonly MySqlConnectionStringValueOption<uint> DnsCheckInterval;

	// Other Options
	public static readonly MySqlConnectionStringValueOption<bool> AllowLoadLocalInfile;
	public static readonly MySqlConnectionStringValueOption<bool> AllowPublicKeyRetrieval;
	public static readonly MySqlConnectionStringValueOption<bool> AllowUserVariables;
	public static readonly MySqlConnectionStringValueOption<bool> AllowZeroDateTime;
	public static readonly MySqlConnectionStringReferenceOption<string> ApplicationName;
	public static readonly MySqlConnectionStringValueOption<bool> AutoEnlist;
	public static readonly MySqlConnectionStringValueOption<int> CancellationTimeout;
	public static readonly MySqlConnectionStringReferenceOption<string> CharacterSet;
	public static readonly MySqlConnectionStringValueOption<uint> ConnectionTimeout;
	public static readonly MySqlConnectionStringValueOption<bool> ConvertZeroDateTime;
	public static readonly MySqlConnectionStringValueOption<MySqlDateTimeKind> DateTimeKind;
	public static readonly MySqlConnectionStringValueOption<uint> DefaultCommandTimeout;
	public static readonly MySqlConnectionStringValueOption<bool> ForceSynchronous;
	public static readonly MySqlConnectionStringValueOption<MySqlGuidFormat> GuidFormat;
	public static readonly MySqlConnectionStringValueOption<bool> IgnoreCommandTransaction;
	public static readonly MySqlConnectionStringValueOption<bool> IgnorePrepare;
	public static readonly MySqlConnectionStringValueOption<bool> InteractiveSession;
	public static readonly MySqlConnectionStringValueOption<uint> Keepalive;
	public static readonly MySqlConnectionStringValueOption<bool> NoBackslashEscapes;
	public static readonly MySqlConnectionStringValueOption<bool> OldGuids;
	public static readonly MySqlConnectionStringValueOption<bool> PersistSecurityInfo;
	public static readonly MySqlConnectionStringValueOption<bool> Pipelining;
	public static readonly MySqlConnectionStringValueOption<MySqlServerRedirectionMode> ServerRedirectionMode;
	public static readonly MySqlConnectionStringReferenceOption<string> ServerRsaPublicKeyFile;
	public static readonly MySqlConnectionStringReferenceOption<string> ServerSPN;
	public static readonly MySqlConnectionStringValueOption<bool> TreatTinyAsBoolean;
	public static readonly MySqlConnectionStringValueOption<bool> UseAffectedRows;
	public static readonly MySqlConnectionStringValueOption<bool> UseCompression;
	public static readonly MySqlConnectionStringValueOption<bool> UseXaTransactions;

	public static MySqlConnectionStringOption? TryGetOptionForKey(string key) =>
		s_options.TryGetValue(key, out var option) ? option : null;

	public static MySqlConnectionStringOption GetOptionForKey(string key) =>
		TryGetOptionForKey(key) ?? throw new ArgumentException($"Option '{key}' not supported.");

	public string Key => m_keys[0];
	public IReadOnlyList<string> Keys => m_keys;

	public abstract object GetObject(MySqlConnectionStringBuilder builder);
	public abstract void SetObject(MySqlConnectionStringBuilder builder, object value);

	protected MySqlConnectionStringOption(IReadOnlyList<string> keys)
	{
		m_keys = keys;
	}

	private static void AddOption(Dictionary<string, MySqlConnectionStringOption> options, MySqlConnectionStringOption option)
	{
		foreach (var key in option.m_keys)
			options.Add(key, option);
		OptionNames.Add(option.m_keys[0]);
	}

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
#pragma warning disable CA1810 // Initialize reference type static fields inline
	static MySqlConnectionStringOption()
	{
		var options = new Dictionary<string, MySqlConnectionStringOption>(StringComparer.OrdinalIgnoreCase);

		// Base Options
#pragma warning disable SA1118 // Parameter should not span multiple lines
		AddOption(options, Server = new(
			keys: ["Server", "Host", "Data Source", "DataSource", "Address", "Addr", "Network Address"],
			defaultValue: ""));

		AddOption(options, Port = new(
			keys: ["Port"],
			defaultValue: 3306u));

		AddOption(options, UserID = new(
			keys: ["User ID", "UserID", "Username", "Uid", "User name", "User"],
			defaultValue: ""));

		AddOption(options, Password = new(
			keys: ["Password", "pwd"],
			defaultValue: ""));

		AddOption(options, Database = new(
			keys: ["Database", "Initial Catalog"],
			defaultValue: ""));

		AddOption(options, LoadBalance = new(
			keys: ["Load Balance", "LoadBalance"],
			defaultValue: MySqlLoadBalance.RoundRobin));

		AddOption(options, ConnectionProtocol = new(
			keys: ["Connection Protocol", "ConnectionProtocol", "Protocol"],
			defaultValue: MySqlConnectionProtocol.Socket));

		AddOption(options, PipeName = new(
			keys: ["Pipe Name", "PipeName", "Pipe"],
			defaultValue: "MYSQL"));

		// SSL/TLS Options
		AddOption(options, SslMode = new(
			keys: ["SSL Mode", "SslMode"],
			defaultValue: MySqlSslMode.Preferred));

		AddOption(options, CertificateFile = new(
			keys: ["Certificate File", "CertificateFile"],
			defaultValue: ""));

		AddOption(options, CertificatePassword = new(
			keys: ["Certificate Password", "CertificatePassword"],
			defaultValue: ""));

		AddOption(options, CertificateStoreLocation = new(
			keys: ["Certificate Store Location", "CertificateStoreLocation"],
			defaultValue: MySqlCertificateStoreLocation.None));

		AddOption(options, CertificateThumbprint = new(
			keys: ["Certificate Thumbprint", "CertificateThumbprint", "Certificate Thumb Print"],
			defaultValue: ""));

		AddOption(options, SslCert = new(
			keys: ["SSL Cert", "SslCert", "Ssl-Cert"],
			defaultValue: ""));

		AddOption(options, SslKey = new(
			keys: ["SSL Key", "SslKey", "Ssl-Key"],
			defaultValue: ""));

		AddOption(options, SslCa = new(
			keys: ["SSL CA", "CACertificateFile", "CA Certificate File", "SslCa", "Ssl-Ca"],
			defaultValue: ""));

		AddOption(options, TlsVersion = new(
			keys: ["TLS Version", "TlsVersion", "Tls-Version"],
			defaultValue: "",
			coerce: value =>
			{
				if (string.IsNullOrWhiteSpace(value))
					return "";

				Span<bool> versions = stackalloc bool[4];
				foreach (var part in value!.TrimStart('[', '(').TrimEnd(')', ']').Split(','))
				{
					var match = TlsVersionsRegex().Match(part);
					if (!match.Success)
						throw new ArgumentException($"Unrecognized TlsVersion protocol version '{part}'; permitted versions are: TLS 1.0, TLS 1.1, TLS 1.2, TLS 1.3.");
					var version = match.Groups[2].Value;
					if (version is "" or "1" or "10" or "1.0")
						versions[0] = true;
					else if (version is "11" or "1.1")
						versions[1] = true;
					else if (version is "12" or "1.2")
						versions[2] = true;
					else if (version is "13" or "1.3")
						versions[3] = true;
				}

				var coercedValue = "";
				Span<char> temp = stackalloc char[7];
				for (var i = 0; i < versions.Length; i++)
				{
					if (versions[i])
					{
						if (coercedValue.Length != 0)
							coercedValue += ", ";
#if NET6_0_OR_GREATER
						coercedValue += string.Create(CultureInfo.InvariantCulture, temp, $"TLS 1.{i}");
#else
						coercedValue += FormattableString.Invariant($"TLS 1.{i}");
#endif
					}
				}
				return coercedValue;
			}));

		AddOption(options, TlsCipherSuites = new(
			keys: ["TLS Cipher Suites", "TlsCipherSuites"],
			defaultValue: ""));

		// Connection Pooling Options
		AddOption(options, Pooling = new(
			keys: ["Pooling"],
			defaultValue: true));

		AddOption(options, ConnectionLifeTime = new(
			keys: ["Connection Lifetime", "ConnectionLifeTime"],
			defaultValue: 0u));

		AddOption(options, ConnectionReset = new(
			keys: ["Connection Reset", "ConnectionReset"],
			defaultValue: true));

		AddOption(options, DeferConnectionReset = new(
			keys: ["Defer Connection Reset", "DeferConnectionReset"],
			defaultValue: true));

		AddOption(options, ConnectionIdlePingTime = new(
			keys: ["Connection Idle Ping Time", "ConnectionIdlePingTime"],
			defaultValue: 0u));

		AddOption(options, ConnectionIdleTimeout = new(
			keys: ["Connection Idle Timeout", "ConnectionIdleTimeout"],
			defaultValue: 180u));

		AddOption(options, MinimumPoolSize = new(
			keys: ["Minimum Pool Size", "Min Pool Size", "MinimumPoolSize", "minpoolsize"],
			defaultValue: 0u));

		AddOption(options, MaximumPoolSize = new(
			keys: ["Maximum Pool Size", "Max Pool Size", "MaximumPoolSize", "maxpoolsize"],
			defaultValue: 100u));

		AddOption(options, DnsCheckInterval = new(
			keys: ["DNS Check Interval", "DnsCheckInterval"],
			defaultValue: 0u));

		// Other Options
		AddOption(options, AllowLoadLocalInfile = new(
			keys: ["Allow Load Local Infile", "AllowLoadLocalInfile"],
			defaultValue: false));

		AddOption(options, AllowPublicKeyRetrieval = new(
			keys: ["Allow Public Key Retrieval", "AllowPublicKeyRetrieval"],
			defaultValue: false));

		AddOption(options, AllowUserVariables = new(
			keys: ["Allow User Variables", "AllowUserVariables"],
			defaultValue: false));

		AddOption(options, AllowZeroDateTime = new(
			keys: ["Allow Zero DateTime", "AllowZeroDateTime"],
			defaultValue: false));

		AddOption(options, ApplicationName = new(
			keys: ["Application Name", "ApplicationName"],
			defaultValue: ""));

		AddOption(options, AutoEnlist = new(
			keys: ["Auto Enlist", "AutoEnlist"],
			defaultValue: true));

		AddOption(options, CancellationTimeout = new(
			keys: ["Cancellation Timeout", "CancellationTimeout"],
			defaultValue: 2,
			coerce: static x =>
			{
				if (x < -1)
					throw new ArgumentOutOfRangeException(nameof(CancellationTimeout), "CancellationTimeout must be greater than or equal to -1");
				return x;
			}));

		AddOption(options, CharacterSet = new(
			keys: ["Character Set", "CharSet", "CharacterSet"],
			defaultValue: ""));

		AddOption(options, ConnectionTimeout = new(
			keys: ["Connection Timeout", "ConnectionTimeout", "Connect Timeout"],
			defaultValue: 15u));

		AddOption(options, ConvertZeroDateTime = new(
			keys: ["Convert Zero DateTime", "ConvertZeroDateTime"],
			defaultValue: false));

		AddOption(options, DateTimeKind = new(
			keys: ["DateTime Kind", "DateTimeKind"],
			defaultValue: MySqlDateTimeKind.Unspecified));

		AddOption(options, DefaultCommandTimeout = new(
			keys: ["Default Command Timeout", "DefaultCommandTimeout", "Command Timeout"],
			defaultValue: 30u));

		AddOption(options, ForceSynchronous = new(
			keys: ["Force Synchronous", "ForceSynchronous"],
			defaultValue: false));

		AddOption(options, GuidFormat = new(
			keys: ["GUID Format", "GuidFormat"],
			defaultValue: MySqlGuidFormat.Default));

		AddOption(options, IgnoreCommandTransaction = new(
			keys: ["Ignore Command Transaction", "IgnoreCommandTransaction"],
			defaultValue: false));

		AddOption(options, IgnorePrepare = new(
			keys: ["Ignore Prepare", "IgnorePrepare"],
			defaultValue: false));

		AddOption(options, InteractiveSession = new(
			keys: ["Interactive Session", "InteractiveSession", "Interactive"],
			defaultValue: false));

		AddOption(options, Keepalive = new(
			keys: ["Keep Alive", "Keepalive"],
			defaultValue: 0u));

		AddOption(options, NoBackslashEscapes = new(
			keys: ["No Backslash Escapes", "NoBackslashEscapes"],
			defaultValue: false));

		AddOption(options, OldGuids = new(
			keys: ["Old Guids", "OldGuids"],
			defaultValue: false));

		AddOption(options, PersistSecurityInfo = new(
			keys: ["Persist Security Info", "PersistSecurityInfo"],
			defaultValue: false));

		AddOption(options, Pipelining = new(
			keys: ["Pipelining"],
			defaultValue: true));

		AddOption(options, ServerRedirectionMode = new(
			keys: ["Server Redirection Mode", "ServerRedirectionMode"],
			defaultValue: MySqlServerRedirectionMode.Disabled));

		AddOption(options, ServerRsaPublicKeyFile = new(
			keys: ["Server RSA Public Key File", "ServerRsaPublicKeyFile"],
			defaultValue: ""));

		AddOption(options, ServerSPN = new(
			keys: ["Server SPN", "ServerSPN"],
			defaultValue: ""));

		AddOption(options, TreatTinyAsBoolean = new(
			keys: ["Treat Tiny As Boolean", "TreatTinyAsBoolean"],
			defaultValue: true));

		AddOption(options, UseAffectedRows = new(
			keys: ["Use Affected Rows", "UseAffectedRows"],
			defaultValue: false));

		AddOption(options, UseCompression = new(
			keys: ["Use Compression", "Compress", "UseCompression"],
			defaultValue: false));

		AddOption(options, UseXaTransactions = new(
			keys: ["Use XA Transactions", "UseXaTransactions"],
			defaultValue: true));
#pragma warning restore SA1118 // Parameter should not span multiple lines

#if NET8_0_OR_GREATER
		s_options = options.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
#else
		s_options = options;
#endif
	}

	private const string c_tlsVersionsRegexPattern = @"\s*TLS( ?v?(1|1\.?0|1\.?1|1\.?2|1\.?3))?$";
#if NET7_0_OR_GREATER
	[GeneratedRegex(c_tlsVersionsRegexPattern, RegexOptions.IgnoreCase)]
	private static partial Regex TlsVersionsRegex();
#else
	private static Regex TlsVersionsRegex() => s_tlsVersionsRegex;
	private static readonly Regex s_tlsVersionsRegex = new(c_tlsVersionsRegexPattern, RegexOptions.IgnoreCase);
#endif
#if NET8_0_OR_GREATER
	private static readonly FrozenDictionary<string, MySqlConnectionStringOption> s_options;
#else
	private static readonly Dictionary<string, MySqlConnectionStringOption> s_options;
#endif

	private readonly IReadOnlyList<string> m_keys;
}

internal sealed class MySqlConnectionStringValueOption<T> : MySqlConnectionStringOption
	where T : struct
{
	public MySqlConnectionStringValueOption(IReadOnlyList<string> keys, T defaultValue, Func<T, T>? coerce = null)
		: base(keys)
	{
		m_defaultValue = defaultValue;
		m_coerce = coerce;
	}

	public T GetValue(MySqlConnectionStringBuilder builder) =>
		builder.TryGetValue(Key, out var objectValue) ? ChangeType(objectValue) : m_defaultValue;

	public void SetValue(MySqlConnectionStringBuilder builder, T value) =>
		builder.DoSetValue(Key, m_coerce is null ? value : m_coerce(value));

	public override object GetObject(MySqlConnectionStringBuilder builder) => GetValue(builder);

	public override void SetObject(MySqlConnectionStringBuilder builder, object value) => SetValue(builder, ChangeType(value));

	private T ChangeType(object objectValue)
	{
		if (typeof(T) == typeof(bool) && objectValue is string booleanString)
		{
			if (string.Equals(booleanString, "yes", StringComparison.OrdinalIgnoreCase))
				return (T) (object) true;
			if (string.Equals(booleanString, "no", StringComparison.OrdinalIgnoreCase))
				return (T) (object) false;
		}

		if ((typeof(T) == typeof(MySqlLoadBalance) || typeof(T) == typeof(MySqlSslMode) || typeof(T) == typeof(MySqlServerRedirectionMode) || typeof(T) == typeof(MySqlDateTimeKind) || typeof(T) == typeof(MySqlGuidFormat) || typeof(T) == typeof(MySqlConnectionProtocol) || typeof(T) == typeof(MySqlCertificateStoreLocation)) && objectValue is string enumString)
		{
			try
			{
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
				return Enum.Parse<T>(enumString, ignoreCase: true);
#else
				return (T) Enum.Parse(typeof(T), enumString, ignoreCase: true);
#endif
			}
			catch (Exception ex) when (ex is not ArgumentException)
			{
#if NET6_0_OR_GREATER
				var exceptionMessage = string.Create(CultureInfo.InvariantCulture, $"Value '{objectValue}' not supported for option '{typeof(T).Name}'.");
#else
				var exceptionMessage = FormattableString.Invariant($"Value '{objectValue}' not supported for option '{typeof(T).Name}'.");
#endif
				throw new ArgumentException(exceptionMessage, ex);
			}
		}

		try
		{
			return (T) Convert.ChangeType(objectValue, typeof(T), CultureInfo.InvariantCulture);
		}
		catch (Exception ex)
		{
#if NET6_0_OR_GREATER
			var exceptionMessage = string.Create(CultureInfo.InvariantCulture, $"Invalid value '{objectValue}' for '{Key}' connection string option.");
#else
			var exceptionMessage = FormattableString.Invariant($"Invalid value '{objectValue}' for '{Key}' connection string option.");
#endif
			throw new ArgumentException(exceptionMessage, ex);
		}
	}

	private readonly T m_defaultValue;
	private readonly Func<T, T>? m_coerce;
}

internal sealed class MySqlConnectionStringReferenceOption<T> : MySqlConnectionStringOption
	where T : class
{
	public MySqlConnectionStringReferenceOption(IReadOnlyList<string> keys, T defaultValue, Func<T?, T>? coerce = null)
		: base(keys)
	{
		m_defaultValue = defaultValue;
		m_coerce = coerce;
	}

	public T GetValue(MySqlConnectionStringBuilder builder) =>
		builder.TryGetValue(Key, out var objectValue) ? ChangeType(objectValue) : m_defaultValue;

	public void SetValue(MySqlConnectionStringBuilder builder, T? value) =>
		builder.DoSetValue(Key, m_coerce is null ? value : m_coerce(value));

	public override object GetObject(MySqlConnectionStringBuilder builder) => GetValue(builder);

	public override void SetObject(MySqlConnectionStringBuilder builder, object value) => SetValue(builder, ChangeType(value));

	private static T ChangeType(object objectValue) =>
		(T) Convert.ChangeType(objectValue, typeof(T), CultureInfo.InvariantCulture);

	private readonly T m_defaultValue;
	private readonly Func<T?, T>? m_coerce;
}
