using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MySqlConnector.Utilities;

namespace MySqlConnector
{
	public sealed class MySqlConnectionStringBuilder : DbConnectionStringBuilder
	{
		public MySqlConnectionStringBuilder()
		{
		}

		public MySqlConnectionStringBuilder(string connectionString)
		{
			ConnectionString = connectionString;
		}

		// Connection Options
		[AllowNull]
		[Category("Connection")]
		[DefaultValue("")]
		[DisplayName("Server")]
		public string Server
		{
			get => MySqlConnectionStringOption.Server.GetValue(this);
			set => MySqlConnectionStringOption.Server.SetValue(this, value);
		}

		[Category("Connection")]
		[DefaultValue(3306u)]
		[DisplayName("Port")]
		public uint Port
		{
			get => MySqlConnectionStringOption.Port.GetValue(this);
			set => MySqlConnectionStringOption.Port.SetValue(this, value);
		}

		[AllowNull]
		[Category("Connection")]
		[DisplayName("User ID")]
		[DefaultValue("")]
		public string UserID
		{
			get => MySqlConnectionStringOption.UserID.GetValue(this);
			set => MySqlConnectionStringOption.UserID.SetValue(this, value);
		}

		[AllowNull]
		[Category("Connection")]
		[DisplayName("Password")]
		[DefaultValue("")]
		public string Password
		{
			get => MySqlConnectionStringOption.Password.GetValue(this);
			set => MySqlConnectionStringOption.Password.SetValue(this, value);
		}

		[AllowNull]
		[Category("Connection")]
		[DisplayName("Database")]
		[DefaultValue("")]
		public string Database
		{
			get => MySqlConnectionStringOption.Database.GetValue(this);
			set => MySqlConnectionStringOption.Database.SetValue(this, value);
		}

		[Category("Connection")]
		[DisplayName("Load Balance")]
		[DefaultValue(MySqlLoadBalance.RoundRobin)]
		public MySqlLoadBalance LoadBalance
		{
			get => MySqlConnectionStringOption.LoadBalance.GetValue(this);
			set => MySqlConnectionStringOption.LoadBalance.SetValue(this, value);
		}

		[Category("Connection")]
		[DisplayName("Connection Protocol")]
		[DefaultValue(MySqlConnectionProtocol.Socket)]
		public MySqlConnectionProtocol ConnectionProtocol
		{
			get => MySqlConnectionStringOption.ConnectionProtocol.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionProtocol.SetValue(this, value);
		}

		[AllowNull]
		[Category("Connection")]
		[DisplayName("Pipe Name")]
		[DefaultValue("MYSQL")]
		public string PipeName
		{
			get => MySqlConnectionStringOption.PipeName.GetValue(this);
			set => MySqlConnectionStringOption.PipeName.SetValue(this, value);
		}

		// SSL/TLS Options
		[Category("TLS")]
		[DisplayName("SSL Mode")]
		[DefaultValue(MySqlSslMode.Preferred)]
		public MySqlSslMode SslMode
		{
			get => MySqlConnectionStringOption.SslMode.GetValue(this);
			set => MySqlConnectionStringOption.SslMode.SetValue(this, value);
		}

		[AllowNull]
		[Category("TLS")]
		[DisplayName("Certificate File")]
		[DefaultValue("")]
		public string CertificateFile
		{
			get => MySqlConnectionStringOption.CertificateFile.GetValue(this);
			set => MySqlConnectionStringOption.CertificateFile.SetValue(this, value);
		}

		[AllowNull]
		[Category("TLS")]
		[DisplayName("Certificate Password")]
		[DefaultValue("")]
		public string CertificatePassword
		{
			get => MySqlConnectionStringOption.CertificatePassword.GetValue(this);
			set => MySqlConnectionStringOption.CertificatePassword.SetValue(this, value);
		}

		[AllowNull]
		[Category("TLS")]
		[DisplayName("SSL Cert")]
		[DefaultValue("")]
		public string SslCert
		{
			get => MySqlConnectionStringOption.SslCert.GetValue(this);
			set => MySqlConnectionStringOption.SslCert.SetValue(this, value);
		}

		[AllowNull]
		[Category("TLS")]
		[DisplayName("SSL Key")]
		[DefaultValue("")]
		public string SslKey
		{
			get => MySqlConnectionStringOption.SslKey.GetValue(this);
			set => MySqlConnectionStringOption.SslKey.SetValue(this, value);
		}

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

		[AllowNull]
		[Category("TLS")]
		[DisplayName("SSL CA")]
		[DefaultValue("")]
		public string SslCa
		{
			get => MySqlConnectionStringOption.SslCa.GetValue(this);
			set => MySqlConnectionStringOption.SslCa.SetValue(this, value);
		}

		[Category("TLS")]
		[DisplayName("Certificate Store Location")]
		[DefaultValue(MySqlCertificateStoreLocation.None)]
		public MySqlCertificateStoreLocation CertificateStoreLocation
		{
			get => MySqlConnectionStringOption.CertificateStoreLocation.GetValue(this);
			set => MySqlConnectionStringOption.CertificateStoreLocation.SetValue(this, value);
		}

		[AllowNull]
		[Category("TLS")]
		[DisplayName("Certificate Thumbprint")]
		[DefaultValue("")]
		public string CertificateThumbprint
		{
			get => MySqlConnectionStringOption.CertificateThumbprint.GetValue(this);
			set => MySqlConnectionStringOption.CertificateThumbprint.SetValue(this, value);
		}

		[AllowNull]
		[Category("TLS")]
		[DisplayName("TLS Version")]
		[DefaultValue("")]
		public string TlsVersion
		{
			get => MySqlConnectionStringOption.TlsVersion.GetValue(this);
			set => MySqlConnectionStringOption.TlsVersion.SetValue(this, value);
		}

		[AllowNull]
		[Category("TLS")]
		[DisplayName("TLS Cipher Suites")]
		[DefaultValue("")]
		public string TlsCipherSuites
		{
			get => MySqlConnectionStringOption.TlsCipherSuites.GetValue(this);
			set => MySqlConnectionStringOption.TlsCipherSuites.SetValue(this, value);
		}

		// Connection Pooling Options
		[Category("Pooling")]
		[DisplayName("Pooling")]
		[DefaultValue(true)]
		public bool Pooling
		{
			get => MySqlConnectionStringOption.Pooling.GetValue(this);
			set => MySqlConnectionStringOption.Pooling.SetValue(this, value);
		}

		[Category("Pooling")]
		[DisplayName("Connection Lifetime")]
		[DefaultValue(0u)]
		public uint ConnectionLifeTime
		{
			get => MySqlConnectionStringOption.ConnectionLifeTime.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionLifeTime.SetValue(this, value);
		}

		[Category("Pooling")]
		[DisplayName("Connection Reset")]
		[DefaultValue(true)]
		public bool ConnectionReset
		{
			get => MySqlConnectionStringOption.ConnectionReset.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionReset.SetValue(this, value);
		}

		[Category("Pooling")]
		[DisplayName("Connection Idle Ping Time")]
		[DefaultValue(0u)]
		public uint ConnectionIdlePingTime
		{
			get => MySqlConnectionStringOption.ConnectionIdlePingTime.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionIdlePingTime.SetValue(this, value);
		}

		[Category("Pooling")]
		[DisplayName("Connection Idle Timeout")]
		[DefaultValue(180u)]
		public uint ConnectionIdleTimeout
		{
			get => MySqlConnectionStringOption.ConnectionIdleTimeout.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionIdleTimeout.SetValue(this, value);
		}

		[Category("Obsolete")]
		[DisplayName("Defer Connection Reset")]
		[DefaultValue(true)]
		[Obsolete("This option is no longer supported in MySqlConnector >= 1.4.0.")]
		public bool DeferConnectionReset
		{
			get => MySqlConnectionStringOption.DeferConnectionReset.GetValue(this);
			set => MySqlConnectionStringOption.DeferConnectionReset.SetValue(this, value);
		}

		[Category("Pooling")]
		[DisplayName("Minimum Pool Size")]
		[DefaultValue(0u)]
		public uint MinimumPoolSize
		{
			get => MySqlConnectionStringOption.MinimumPoolSize.GetValue(this);
			set => MySqlConnectionStringOption.MinimumPoolSize.SetValue(this, value);
		}

		[Category("Pooling")]
		[DisplayName("Maximum Pool Size")]
		[DefaultValue(100u)]
		public uint MaximumPoolSize
		{
			get => MySqlConnectionStringOption.MaximumPoolSize.GetValue(this);
			set => MySqlConnectionStringOption.MaximumPoolSize.SetValue(this, value);
		}

		// Other Options
		[Category("Other")]
		[DisplayName("Allow Load Local Infile")]
		[DefaultValue(false)]
		public bool AllowLoadLocalInfile
		{
			get => MySqlConnectionStringOption.AllowLoadLocalInfile.GetValue(this);
			set => MySqlConnectionStringOption.AllowLoadLocalInfile.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Allow Public Key Retrieval")]
		[DefaultValue(false)]
		public bool AllowPublicKeyRetrieval
		{
			get => MySqlConnectionStringOption.AllowPublicKeyRetrieval.GetValue(this);
			set => MySqlConnectionStringOption.AllowPublicKeyRetrieval.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Allow User Variables")]
		[DefaultValue(false)]
		public bool AllowUserVariables
		{
			get => MySqlConnectionStringOption.AllowUserVariables.GetValue(this);
			set => MySqlConnectionStringOption.AllowUserVariables.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Allow Zero DateTime")]
		[DefaultValue(false)]
		public bool AllowZeroDateTime
		{
			get => MySqlConnectionStringOption.AllowZeroDateTime.GetValue(this);
			set => MySqlConnectionStringOption.AllowZeroDateTime.SetValue(this, value);
		}

		[AllowNull]
		[Category("Other")]
		[DisplayName("Application Name")]
		[DefaultValue("")]
		public string ApplicationName
		{
			get => MySqlConnectionStringOption.ApplicationName.GetValue(this);
			set => MySqlConnectionStringOption.ApplicationName.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Auto Enlist")]
		[DefaultValue(true)]
		public bool AutoEnlist
		{
			get => MySqlConnectionStringOption.AutoEnlist.GetValue(this);
			set => MySqlConnectionStringOption.AutoEnlist.SetValue(this, value);
		}

		[AllowNull]
		[Category("Other")]
		[DisplayName("Character Set")]
		[DefaultValue("")]
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
		[DisplayName("Connection Timeout")]
		[DefaultValue(15u)]
		public uint ConnectionTimeout
		{
			get => MySqlConnectionStringOption.ConnectionTimeout.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionTimeout.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Convert Zero DateTime")]
		[DefaultValue(false)]
		public bool ConvertZeroDateTime
		{
			get => MySqlConnectionStringOption.ConvertZeroDateTime.GetValue(this);
			set => MySqlConnectionStringOption.ConvertZeroDateTime.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("DateTime Kind")]
		[DefaultValue(MySqlDateTimeKind.Unspecified)]
		public MySqlDateTimeKind DateTimeKind
		{
			get => MySqlConnectionStringOption.DateTimeKind.GetValue(this);
			set => MySqlConnectionStringOption.DateTimeKind.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Default Command Timeout")]
		[DefaultValue(30u)]
		public uint DefaultCommandTimeout
		{
			get => MySqlConnectionStringOption.DefaultCommandTimeout.GetValue(this);
			set => MySqlConnectionStringOption.DefaultCommandTimeout.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Cancellation Timeout")]
		[DefaultValue(2)]
		public int CancellationTimeout
		{
			get => MySqlConnectionStringOption.CancellationTimeout.GetValue(this);
			set => MySqlConnectionStringOption.CancellationTimeout.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Force Synchronous")]
		[DefaultValue(false)]
		public bool ForceSynchronous
		{
			get => MySqlConnectionStringOption.ForceSynchronous.GetValue(this);
			set => MySqlConnectionStringOption.ForceSynchronous.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("GUID Format")]
		[DefaultValue(MySqlGuidFormat.Default)]
		public MySqlGuidFormat GuidFormat
		{
			get => MySqlConnectionStringOption.GuidFormat.GetValue(this);
			set => MySqlConnectionStringOption.GuidFormat.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Ignore Command Transaction")]
		[DefaultValue(false)]
		public bool IgnoreCommandTransaction
		{
			get => MySqlConnectionStringOption.IgnoreCommandTransaction.GetValue(this);
			set => MySqlConnectionStringOption.IgnoreCommandTransaction.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Ignore Prepare")]
		[DefaultValue(false)]
		public bool IgnorePrepare
		{
			get => MySqlConnectionStringOption.IgnorePrepare.GetValue(this);
			set => MySqlConnectionStringOption.IgnorePrepare.SetValue(this, value);
		}

		[Category("Connection")]
		[DisplayName("Interactive Session")]
		[DefaultValue(false)]
		public bool InteractiveSession
		{
			get => MySqlConnectionStringOption.InteractiveSession.GetValue(this);
			set => MySqlConnectionStringOption.InteractiveSession.SetValue(this, value);
		}

		[Category("Connection")]
		[DisplayName("Keep Alive")]
		[DefaultValue(0u)]
		public uint Keepalive
		{
			get => MySqlConnectionStringOption.Keepalive.GetValue(this);
			set => MySqlConnectionStringOption.Keepalive.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("No Backslash Escapes")]
		[DefaultValue(false)]
		public bool NoBackslashEscapes
		{
			get => MySqlConnectionStringOption.NoBackslashEscapes.GetValue(this);
			set => MySqlConnectionStringOption.NoBackslashEscapes.SetValue(this, value);
		}

		[Category("Obsolete")]
		[DisplayName("Old Guids")]
		[DefaultValue(false)]
		public bool OldGuids
		{
			get => MySqlConnectionStringOption.OldGuids.GetValue(this);
			set => MySqlConnectionStringOption.OldGuids.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Persist Security Info")]
		[DefaultValue(false)]
		public bool PersistSecurityInfo
		{
			get => MySqlConnectionStringOption.PersistSecurityInfo.GetValue(this);
			set => MySqlConnectionStringOption.PersistSecurityInfo.SetValue(this, value);
		}

		[Category("Connection")]
		[DisplayName("Server Redirection Mode")]
		[DefaultValue(MySqlServerRedirectionMode.Disabled)]
		public MySqlServerRedirectionMode ServerRedirectionMode
		{
			get => MySqlConnectionStringOption.ServerRedirectionMode.GetValue(this);
			set => MySqlConnectionStringOption.ServerRedirectionMode.SetValue(this, value);
		}

		[AllowNull]
		[Category("Connection")]
		[DisplayName("Server RSA Public Key File")]
		[DefaultValue("")]
		public string ServerRsaPublicKeyFile
		{
			get => MySqlConnectionStringOption.ServerRsaPublicKeyFile.GetValue(this);
			set => MySqlConnectionStringOption.ServerRsaPublicKeyFile.SetValue(this, value);
		}

		[AllowNull]
		[Category("Connection")]
		[DisplayName("Server SPN")]
		[DefaultValue("")]
		public string ServerSPN
		{
			get => MySqlConnectionStringOption.ServerSPN.GetValue(this);
			set => MySqlConnectionStringOption.ServerSPN.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Treat Tiny As Boolean")]
		[DefaultValue(true)]
		public bool TreatTinyAsBoolean
		{
			get => MySqlConnectionStringOption.TreatTinyAsBoolean.GetValue(this);
			set => MySqlConnectionStringOption.TreatTinyAsBoolean.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Use Affected Rows")]
		[DefaultValue(false)]
		public bool UseAffectedRows
		{
			get => MySqlConnectionStringOption.UseAffectedRows.GetValue(this);
			set => MySqlConnectionStringOption.UseAffectedRows.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Use Compression")]
		[DefaultValue(false)]
		public bool UseCompression
		{
			get => MySqlConnectionStringOption.UseCompression.GetValue(this);
			set => MySqlConnectionStringOption.UseCompression.SetValue(this, value);
		}

		[Category("Other")]
		[DisplayName("Use XA Transactions")]
		[DefaultValue(true)]
		public bool UseXaTransactions
		{
			get => MySqlConnectionStringOption.UseXaTransactions.GetValue(this);
			set => MySqlConnectionStringOption.UseXaTransactions.SetValue(this, value);
		}

		// Other Methods
		public override bool ContainsKey(string keyword)
		{
			var option = MySqlConnectionStringOption.TryGetOptionForKey(keyword);
			return option is object && base.ContainsKey(option.Key);
		}

		public override bool Remove(string keyword)
		{
			var option = MySqlConnectionStringOption.TryGetOptionForKey(keyword);
			return option is object && base.Remove(option.Key);
		}

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
					foreach (var passwordKey in MySqlConnectionStringOption.Password.Keys)
						if (string.Equals(key, passwordKey, StringComparison.OrdinalIgnoreCase))
							csb.Remove(key!);
				m_cachedConnectionStringWithoutPassword = csb.ConnectionString;
				m_cachedConnectionString = connectionString;
			}

			return m_cachedConnectionStringWithoutPassword!;
		}

		protected override void GetProperties(Hashtable propertyDescriptors)
		{
			base.GetProperties(propertyDescriptors);

			// only report properties with a [Category] attribute that are not [Obsolete]
			var propertiesToRemove = propertyDescriptors.Values
				.Cast<PropertyDescriptor>()
				.Where(x => !x.Attributes.OfType<CategoryAttribute>().Any() || x.Attributes.OfType<ObsoleteAttribute>().Any())
				.ToList();
			foreach (var property in propertiesToRemove)
				propertyDescriptors.Remove(property.DisplayName);
		}

		string? m_cachedConnectionString;
		string? m_cachedConnectionStringWithoutPassword;
	}

	internal abstract class MySqlConnectionStringOption
	{
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
		public static readonly MySqlConnectionStringReferenceOption<string> SslCa;
		public static readonly MySqlConnectionStringReferenceOption<string> SslCert;
		public static readonly MySqlConnectionStringReferenceOption<string> SslKey;
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
			TryGetOptionForKey(key) ?? throw new ArgumentException("Option '{0}' not supported.".FormatInvariant(key));

		public string Key => m_keys[0];
		public IReadOnlyList<string> Keys => m_keys;

		public abstract object GetObject(MySqlConnectionStringBuilder builder);
		public abstract void SetObject(MySqlConnectionStringBuilder builder, object value);

		protected MySqlConnectionStringOption(IReadOnlyList<string> keys)
		{
			m_keys = keys;
		}

		private static void AddOption(MySqlConnectionStringOption option)
		{
			foreach (string key in option.m_keys)
				s_options.Add(key, option);
		}

		static MySqlConnectionStringOption()
		{
			s_options = new(StringComparer.OrdinalIgnoreCase);

			// Base Options
			AddOption(Server = new(
				keys: new[] { "Server", "Host", "Data Source", "DataSource", "Address", "Addr", "Network Address" },
				defaultValue: ""));

			AddOption(Port = new(
				keys: new[] { "Port" },
				defaultValue: 3306u));

			AddOption(UserID = new(
				keys: new[] { "User ID", "UserID", "Username", "Uid", "User name", "User" },
				defaultValue: ""));

			AddOption(Password = new(
				keys: new[] { "Password", "pwd" },
				defaultValue: ""));

			AddOption(Database = new(
				keys: new[] { "Database", "Initial Catalog" },
				defaultValue: ""));

			AddOption(LoadBalance = new(
				keys: new[] { "Load Balance", "LoadBalance" },
				defaultValue: MySqlLoadBalance.RoundRobin));

			AddOption(ConnectionProtocol = new(
				keys: new[] { "Connection Protocol", "ConnectionProtocol", "Protocol" },
				defaultValue: MySqlConnectionProtocol.Socket));

			AddOption(PipeName = new(
				keys: new[] { "Pipe Name", "PipeName", "Pipe" },
				defaultValue: "MYSQL"));

			// SSL/TLS Options
			AddOption(SslMode = new(
				keys: new[] { "SSL Mode", "SslMode" },
				defaultValue: MySqlSslMode.Preferred));

			AddOption(CertificateFile = new(
				keys: new[] { "Certificate File", "CertificateFile" },
				defaultValue: ""));

			AddOption(CertificatePassword = new(
				keys: new[] { "Certificate Password", "CertificatePassword" },
				defaultValue: ""));

			AddOption(SslCa = new(
				keys: new[] { "SSL CA", "CACertificateFile", "CA Certificate File", "SslCa", "Ssl-Ca" },
				defaultValue: ""));

			AddOption(SslCert = new(
				keys: new[] { "SSL Cert", "SslCert", "Ssl-Cert" },
				defaultValue: ""));

			AddOption(SslKey = new(
				keys: new[] { "SSL Key", "SslKey", "Ssl-Key" },
				defaultValue: ""));

			AddOption(CertificateStoreLocation = new(
				keys: new[] { "Certificate Store Location", "CertificateStoreLocation" },
				defaultValue: MySqlCertificateStoreLocation.None));

			AddOption(CertificateThumbprint = new(
				keys: new[] { "Certificate Thumbprint", "CertificateThumbprint", "Certificate Thumb Print" },
				defaultValue: ""));

			AddOption(TlsVersion = new(
				keys: new[] { "TLS Version", "TlsVersion", "Tls-Version" },
				defaultValue: "",
				coerce: value =>
				{
					if (string.IsNullOrWhiteSpace(value))
						return "";

					Span<bool> versions = stackalloc bool[4];
					foreach (var part in value!.TrimStart('[', '(').TrimEnd(')', ']').Split(','))
					{
						var match = Regex.Match(part, @"\s*TLS( ?v?(1|1\.?0|1\.?1|1\.?2|1\.?3))?$", RegexOptions.IgnoreCase);
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
					for (var i = 0; i < versions.Length; i++)
					{
						if (versions[i])
						{
							if (coercedValue.Length != 0)
								coercedValue += ", ";
							coercedValue += "TLS 1.{0}".FormatInvariant(i);
						}
					}
					return coercedValue;
				}));

			AddOption(TlsCipherSuites = new(
				keys: new[] { "TLS Cipher Suites", "TlsCipherSuites" },
				defaultValue: ""));

			// Connection Pooling Options
			AddOption(Pooling = new(
				keys: new[] { "Pooling" },
				defaultValue: true));

			AddOption(ConnectionLifeTime = new(
				keys: new[] { "Connection Lifetime", "ConnectionLifeTime" },
				defaultValue: 0u));

			AddOption(ConnectionReset = new(
				keys: new[] { "Connection Reset", "ConnectionReset" },
				defaultValue: true));

			AddOption(DeferConnectionReset = new(
				keys: new[] { "Defer Connection Reset", "DeferConnectionReset" },
				defaultValue: true));

			AddOption(ConnectionIdlePingTime = new(
				keys: new[] { "Connection Idle Ping Time", "ConnectionIdlePingTime" },
				defaultValue: 0u));

			AddOption(ConnectionIdleTimeout = new(
				keys: new[] { "Connection Idle Timeout", "ConnectionIdleTimeout" },
				defaultValue: 180u));

			AddOption(MinimumPoolSize = new(
				keys: new[] { "Minimum Pool Size", "Min Pool Size", "MinimumPoolSize", "minpoolsize" },
				defaultValue: 0u));

			AddOption(MaximumPoolSize = new(
				keys: new[] { "Maximum Pool Size", "Max Pool Size", "MaximumPoolSize", "maxpoolsize" },
				defaultValue: 100u));

			// Other Options
			AddOption(AllowLoadLocalInfile = new(
				keys: new[] { "Allow Load Local Infile", "AllowLoadLocalInfile" },
				defaultValue: false));

			AddOption(AllowPublicKeyRetrieval = new(
				keys: new[] { "Allow Public Key Retrieval", "AllowPublicKeyRetrieval" },
				defaultValue: false));

			AddOption(AllowUserVariables = new(
				keys: new[] { "Allow User Variables", "AllowUserVariables" },
				defaultValue: false));

			AddOption(AllowZeroDateTime = new(
				keys: new[] { "Allow Zero DateTime", "AllowZeroDateTime" },
				defaultValue: false));

			AddOption(ApplicationName = new(
				keys: new[] { "Application Name", "ApplicationName" },
				defaultValue: ""));

			AddOption(AutoEnlist = new(
				keys: new[] { "Auto Enlist", "AutoEnlist" },
				defaultValue: true));

			AddOption(CancellationTimeout = new(
				keys: new[] { "Cancellation Timeout", "CancellationTimeout" },
				defaultValue: 2,
				coerce: x =>
				{
					if (x < -1)
						throw new ArgumentOutOfRangeException(nameof(CancellationTimeout), "CancellationTimeout must be greater than or equal to -1");
					return x;
				}));

			AddOption(CharacterSet = new(
				keys: new[] { "Character Set", "CharSet", "CharacterSet" },
				defaultValue: ""));

			AddOption(ConnectionTimeout = new(
				keys: new[] { "Connection Timeout", "ConnectionTimeout", "Connect Timeout" },
				defaultValue: 15u));

			AddOption(ConvertZeroDateTime = new(
				keys: new[] { "Convert Zero DateTime", "ConvertZeroDateTime" },
				defaultValue: false));

			AddOption(DateTimeKind = new(
				keys: new[] { "DateTime Kind", "DateTimeKind" },
				defaultValue: MySqlDateTimeKind.Unspecified));

			AddOption(DefaultCommandTimeout = new(
				keys: new[] { "Default Command Timeout", "DefaultCommandTimeout", "Command Timeout" },
				defaultValue: 30u));

			AddOption(ForceSynchronous = new(
				keys: new[] { "Force Synchronous", "ForceSynchronous" },
				defaultValue: false));

			AddOption(GuidFormat = new(
				keys: new[] { "GUID Format", "GuidFormat" },
				defaultValue: MySqlGuidFormat.Default));

			AddOption(IgnoreCommandTransaction = new(
				keys: new[] { "Ignore Command Transaction", "IgnoreCommandTransaction" },
				defaultValue: false));

			AddOption(IgnorePrepare = new(
				keys: new[] { "Ignore Prepare", "IgnorePrepare" },
				defaultValue: false));

			AddOption(InteractiveSession = new(
				keys: new[] { "Interactive Session", "InteractiveSession", "Interactive" },
				defaultValue: false));

			AddOption(Keepalive = new(
				keys: new[] { "Keep Alive", "Keepalive" },
				defaultValue: 0u));

			AddOption(NoBackslashEscapes = new(
				keys: new[] { "No Backslash Escapes", "NoBackslashEscapes" },
				defaultValue: false));

			AddOption(OldGuids = new(
				keys: new[] { "Old Guids", "OldGuids" },
				defaultValue: false));

			AddOption(PersistSecurityInfo = new(
				keys: new[] { "Persist Security Info", "PersistSecurityInfo" },
				defaultValue: false));

			AddOption(ServerRedirectionMode = new(
				keys: new[] { "Server Redirection Mode", "ServerRedirectionMode" },
				defaultValue: MySqlServerRedirectionMode.Disabled));

			AddOption(ServerRsaPublicKeyFile = new(
				keys: new[] { "Server RSA Public Key File", "ServerRsaPublicKeyFile" },
				defaultValue: ""));

			AddOption(ServerSPN = new(
				keys: new[] { "Server SPN", "ServerSPN" },
				defaultValue: ""));

			AddOption(TreatTinyAsBoolean = new(
				keys: new[] { "Treat Tiny As Boolean", "TreatTinyAsBoolean" },
				defaultValue: true));

			AddOption(UseAffectedRows = new(
				keys: new[] { "Use Affected Rows", "UseAffectedRows" },
				defaultValue: false));

			AddOption(UseCompression = new(
				keys: new[] { "Use Compression", "Compress", "UseCompression" },
				defaultValue: false));

			AddOption(UseXaTransactions = new(
				keys: new[] { "Use XA Transactions", "UseXaTransactions" },
				defaultValue: true));
		}

		static readonly Dictionary<string, MySqlConnectionStringOption> s_options;

		readonly IReadOnlyList<string> m_keys;
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
					throw new ArgumentException("Value '{0}' not supported for option '{1}'.".FormatInvariant(objectValue, typeof(T).Name), ex);
				}
			}

			try
			{
				return (T) Convert.ChangeType(objectValue, typeof(T), CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Invalid value '{0}' for '{1}' connection string option.".FormatInvariant(objectValue, Key), ex);
			}
		}

		readonly T m_defaultValue;
		readonly Func<T, T>? m_coerce;
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

		readonly T m_defaultValue;
		readonly Func<T?, T>? m_coerce;
	}
}
