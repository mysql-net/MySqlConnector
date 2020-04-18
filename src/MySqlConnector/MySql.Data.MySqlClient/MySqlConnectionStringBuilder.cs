using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using MySqlConnector.Utilities;

namespace MySql.Data.MySqlClient
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

		// Base Options
		public string Server
		{
			get => MySqlConnectionStringOption.Server.GetValue(this);
			set => MySqlConnectionStringOption.Server.SetValue(this, value);
		}

		public uint Port
		{
			get => MySqlConnectionStringOption.Port.GetValue(this);
			set => MySqlConnectionStringOption.Port.SetValue(this, value);
		}

		public string UserID
		{
			get => MySqlConnectionStringOption.UserID.GetValue(this);
			set => MySqlConnectionStringOption.UserID.SetValue(this, value);
		}

		public string Password
		{
			get => MySqlConnectionStringOption.Password.GetValue(this);
			set => MySqlConnectionStringOption.Password.SetValue(this, value);
		}

		public string Database
		{
			get => MySqlConnectionStringOption.Database.GetValue(this);
			set => MySqlConnectionStringOption.Database.SetValue(this, value);
		}

		public MySqlLoadBalance LoadBalance
		{
			get => MySqlConnectionStringOption.LoadBalance.GetValue(this);
			set => MySqlConnectionStringOption.LoadBalance.SetValue(this, value);
		}

		public MySqlConnectionProtocol ConnectionProtocol
		{
			get => MySqlConnectionStringOption.ConnectionProtocol.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionProtocol.SetValue(this, value);
		}

		public string? PipeName
		{
			get => MySqlConnectionStringOption.PipeName.GetValue(this);
			set => MySqlConnectionStringOption.PipeName.SetValue(this, value);
		}

		// SSL/TLS Options
		public MySqlSslMode SslMode
		{
			get => MySqlConnectionStringOption.SslMode.GetValue(this);
			set => MySqlConnectionStringOption.SslMode.SetValue(this, value);
		}

		public string? CertificateFile
		{
			get => MySqlConnectionStringOption.CertificateFile.GetValue(this);
			set => MySqlConnectionStringOption.CertificateFile.SetValue(this, value);
		}

		public string? CertificatePassword
		{
			get => MySqlConnectionStringOption.CertificatePassword.GetValue(this);
			set => MySqlConnectionStringOption.CertificatePassword.SetValue(this, value);
		}

		public string? SslCert
		{
			get => MySqlConnectionStringOption.SslCert.GetValue(this);
			set => MySqlConnectionStringOption.SslCert.SetValue(this, value);
		}

		public string? SslKey
		{
			get => MySqlConnectionStringOption.SslKey.GetValue(this);
			set => MySqlConnectionStringOption.SslKey.SetValue(this, value);
		}

		[Obsolete("Use SslCa instead.")]
		public string? CACertificateFile
		{
			get => MySqlConnectionStringOption.SslCa.GetValue(this);
			set => MySqlConnectionStringOption.SslCa.SetValue(this, value);
		}

		public string? SslCa
		{
			get => MySqlConnectionStringOption.SslCa.GetValue(this);
			set => MySqlConnectionStringOption.SslCa.SetValue(this, value);
		}

		public MySqlCertificateStoreLocation CertificateStoreLocation
		{
			get => MySqlConnectionStringOption.CertificateStoreLocation.GetValue(this);
			set => MySqlConnectionStringOption.CertificateStoreLocation.SetValue(this, value);
		}

		public string? CertificateThumbprint
		{
			get => MySqlConnectionStringOption.CertificateThumbprint.GetValue(this);
			set => MySqlConnectionStringOption.CertificateThumbprint.SetValue(this, value);
		}

		// Connection Pooling Options
		public bool Pooling
		{
			get => MySqlConnectionStringOption.Pooling.GetValue(this);
			set => MySqlConnectionStringOption.Pooling.SetValue(this, value);
		}

		public uint ConnectionLifeTime
		{
			get => MySqlConnectionStringOption.ConnectionLifeTime.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionLifeTime.SetValue(this, value);
		}

		public bool ConnectionReset
		{
			get => MySqlConnectionStringOption.ConnectionReset.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionReset.SetValue(this, value);
		}

		public uint ConnectionIdlePingTime
		{
			get => MySqlConnectionStringOption.ConnectionIdlePingTime.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionIdlePingTime.SetValue(this, value);
		}

		public uint ConnectionIdleTimeout
		{
			get => MySqlConnectionStringOption.ConnectionIdleTimeout.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionIdleTimeout.SetValue(this, value);
		}

		public uint MinimumPoolSize
		{
			get => MySqlConnectionStringOption.MinimumPoolSize.GetValue(this);
			set => MySqlConnectionStringOption.MinimumPoolSize.SetValue(this, value);
		}

		public uint MaximumPoolSize
		{
			get => MySqlConnectionStringOption.MaximumPoolSize.GetValue(this);
			set => MySqlConnectionStringOption.MaximumPoolSize.SetValue(this, value);
		}

		// Other Options
		public bool AllowLoadLocalInfile
		{
			get => MySqlConnectionStringOption.AllowLoadLocalInfile.GetValue(this);
			set => MySqlConnectionStringOption.AllowLoadLocalInfile.SetValue(this, value);
		}

		public bool AllowPublicKeyRetrieval
		{
			get => MySqlConnectionStringOption.AllowPublicKeyRetrieval.GetValue(this);
			set => MySqlConnectionStringOption.AllowPublicKeyRetrieval.SetValue(this, value);
		}

		public bool AllowUserVariables
		{
			get => MySqlConnectionStringOption.AllowUserVariables.GetValue(this);
			set => MySqlConnectionStringOption.AllowUserVariables.SetValue(this, value);
		}

		public bool AllowZeroDateTime
		{
			get => MySqlConnectionStringOption.AllowZeroDateTime.GetValue(this);
			set => MySqlConnectionStringOption.AllowZeroDateTime.SetValue(this, value);
		}

		public string? ApplicationName
		{
			get => MySqlConnectionStringOption.ApplicationName.GetValue(this);
			set => MySqlConnectionStringOption.ApplicationName.SetValue(this, value);
		}

		public bool AutoEnlist
		{
			get => MySqlConnectionStringOption.AutoEnlist.GetValue(this);
			set => MySqlConnectionStringOption.AutoEnlist.SetValue(this, value);
		}

		public string? CharacterSet
		{
			get => MySqlConnectionStringOption.CharacterSet.GetValue(this);
			set => MySqlConnectionStringOption.CharacterSet.SetValue(this, value);
		}

		/// <summary>
		/// The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.
		/// The default value is 15.
		/// </summary>
		public uint ConnectionTimeout
		{
			get => MySqlConnectionStringOption.ConnectionTimeout.GetValue(this);
			set => MySqlConnectionStringOption.ConnectionTimeout.SetValue(this, value);
		}

		public bool ConvertZeroDateTime
		{
			get => MySqlConnectionStringOption.ConvertZeroDateTime.GetValue(this);
			set => MySqlConnectionStringOption.ConvertZeroDateTime.SetValue(this, value);
		}

		public MySqlDateTimeKind DateTimeKind
		{
			get => MySqlConnectionStringOption.DateTimeKind.GetValue(this);
			set => MySqlConnectionStringOption.DateTimeKind.SetValue(this, value);
		}

		public uint DefaultCommandTimeout
		{
			get => MySqlConnectionStringOption.DefaultCommandTimeout.GetValue(this);
			set => MySqlConnectionStringOption.DefaultCommandTimeout.SetValue(this, value);
		}

		public bool ForceSynchronous
		{
			get => MySqlConnectionStringOption.ForceSynchronous.GetValue(this);
			set => MySqlConnectionStringOption.ForceSynchronous.SetValue(this, value);
		}

		public MySqlGuidFormat GuidFormat
		{
			get => MySqlConnectionStringOption.GuidFormat.GetValue(this);
			set => MySqlConnectionStringOption.GuidFormat.SetValue(this, value);
		}

		public bool IgnoreCommandTransaction
		{
			get => MySqlConnectionStringOption.IgnoreCommandTransaction.GetValue(this);
			set => MySqlConnectionStringOption.IgnoreCommandTransaction.SetValue(this, value);
		}

		public bool IgnorePrepare
		{
			get => MySqlConnectionStringOption.IgnorePrepare.GetValue(this);
			set => MySqlConnectionStringOption.IgnorePrepare.SetValue(this, value);
		}

		public bool InteractiveSession
		{
			get => MySqlConnectionStringOption.InteractiveSession.GetValue(this);
			set => MySqlConnectionStringOption.InteractiveSession.SetValue(this, value);
		}

		public uint Keepalive
		{
			get => MySqlConnectionStringOption.Keepalive.GetValue(this);
			set => MySqlConnectionStringOption.Keepalive.SetValue(this, value);
		}

		public bool NoBackslashEscapes
		{
			get => MySqlConnectionStringOption.NoBackslashEscapes.GetValue(this);
			set => MySqlConnectionStringOption.NoBackslashEscapes.SetValue(this, value);
		}

		public bool OldGuids
		{
			get => MySqlConnectionStringOption.OldGuids.GetValue(this);
			set => MySqlConnectionStringOption.OldGuids.SetValue(this, value);
		}

		public bool PersistSecurityInfo
		{
			get => MySqlConnectionStringOption.PersistSecurityInfo.GetValue(this);
			set => MySqlConnectionStringOption.PersistSecurityInfo.SetValue(this, value);
		}

		public string? ServerRsaPublicKeyFile
		{
			get => MySqlConnectionStringOption.ServerRsaPublicKeyFile.GetValue(this);
			set => MySqlConnectionStringOption.ServerRsaPublicKeyFile.SetValue(this, value);
		}

		public string? ServerSPN
		{
			get => MySqlConnectionStringOption.ServerSPN.GetValue(this);
			set => MySqlConnectionStringOption.ServerSPN.SetValue(this, value);
		}

		public bool TreatTinyAsBoolean
		{
			get => MySqlConnectionStringOption.TreatTinyAsBoolean.GetValue(this);
			set => MySqlConnectionStringOption.TreatTinyAsBoolean.SetValue(this, value);
		}

		public bool UseAffectedRows
		{
			get => MySqlConnectionStringOption.UseAffectedRows.GetValue(this);
			set => MySqlConnectionStringOption.UseAffectedRows.SetValue(this, value);
		}

		public bool UseCompression
		{
			get => MySqlConnectionStringOption.UseCompression.GetValue(this);
			set => MySqlConnectionStringOption.UseCompression.SetValue(this, value);
		}

		public bool UseXaTransactions
		{
			get => MySqlConnectionStringOption.UseXaTransactions.GetValue(this);
			set => MySqlConnectionStringOption.UseXaTransactions.SetValue(this, value);
		}

		// Other Methods
		public override bool ContainsKey(string key)
		{
			var option = MySqlConnectionStringOption.TryGetOptionForKey(key);
			return option is object && base.ContainsKey(option.Key);
		}

		public override bool Remove(string key)
		{
			var option = MySqlConnectionStringOption.TryGetOptionForKey(key);
			return option is object && base.Remove(option.Key);
		}

		public override object? this[string key]
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

		string? m_cachedConnectionString;
		string? m_cachedConnectionStringWithoutPassword;
	}

	internal abstract class MySqlConnectionStringOption
	{
		// Base Options
		public static readonly MySqlConnectionStringNonNullOption<string> Server;
		public static readonly MySqlConnectionStringValueOption<uint> Port;
		public static readonly MySqlConnectionStringNonNullOption<string> UserID;
		public static readonly MySqlConnectionStringNonNullOption<string> Password;
		public static readonly MySqlConnectionStringNonNullOption<string> Database;
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

		// Connection Pooling Options
		public static readonly MySqlConnectionStringValueOption<bool> Pooling;
		public static readonly MySqlConnectionStringValueOption<uint> ConnectionLifeTime;
		public static readonly MySqlConnectionStringValueOption<bool> ConnectionReset;
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

		public abstract object? GetObject(MySqlConnectionStringBuilder builder);
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
			s_options = new Dictionary<string, MySqlConnectionStringOption>(StringComparer.OrdinalIgnoreCase);

			// Base Options
			AddOption(Server = new MySqlConnectionStringNonNullOption<string>(
				keys: new[] { "Server", "Host", "Data Source", "DataSource", "Address", "Addr", "Network Address" },
				defaultValue: ""));

			AddOption(Port = new MySqlConnectionStringValueOption<uint>(
				keys: new[] { "Port" },
				defaultValue: 3306u));

			AddOption(UserID = new MySqlConnectionStringNonNullOption<string>(
				keys: new[] { "User Id", "UserID", "Username", "Uid", "User name", "User" },
				defaultValue: ""));

			AddOption(Password = new MySqlConnectionStringNonNullOption<string>(
				keys: new[] { "Password", "pwd" },
				defaultValue: ""));

			AddOption(Database = new MySqlConnectionStringNonNullOption<string>(
				keys: new[] { "Database", "Initial Catalog" },
				defaultValue: ""));

			AddOption(LoadBalance = new MySqlConnectionStringValueOption<MySqlLoadBalance>(
				keys: new[] { "LoadBalance", "Load Balance" },
				defaultValue: MySqlLoadBalance.RoundRobin));

			AddOption(ConnectionProtocol = new MySqlConnectionStringValueOption<MySqlConnectionProtocol>(
				keys: new[] { "ConnectionProtocol", "Connection Protocol", "Protocol" },
				defaultValue: MySqlConnectionProtocol.Socket));

			AddOption(PipeName = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "PipeName", "Pipe", "Pipe Name" },
				defaultValue: "MYSQL"));

			// SSL/TLS Options
			AddOption(SslMode = new MySqlConnectionStringValueOption<MySqlSslMode>(
				keys: new[] { "SSL Mode", "SslMode" },
				defaultValue: MySqlSslMode.Preferred));

			AddOption(CertificateFile = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "CertificateFile", "Certificate File" },
				defaultValue: null));

			AddOption(CertificatePassword = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "CertificatePassword", "Certificate Password" },
				defaultValue: null));

			AddOption(SslCa = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "CACertificateFile", "CA Certificate File", "SslCa", "Ssl-Ca" },
				defaultValue: null));

			AddOption(SslCert = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "SslCert", "Ssl-Cert" },
				defaultValue: null));

			AddOption(SslKey = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "SslKey", "Ssl-Key" },
				defaultValue: null));

			AddOption(CertificateStoreLocation = new MySqlConnectionStringValueOption<MySqlCertificateStoreLocation>(
				keys: new[] { "CertificateStoreLocation", "Certificate Store Location" },
				defaultValue: MySqlCertificateStoreLocation.None));

			AddOption(CertificateThumbprint = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "CertificateThumbprint", "Certificate Thumbprint", "Certificate Thumb Print" },
				defaultValue: null));

			// Connection Pooling Options
			AddOption(Pooling = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "Pooling" },
				defaultValue: true));

			AddOption(ConnectionLifeTime = new MySqlConnectionStringValueOption<uint>(
				keys: new[] { "Connection Lifetime", "ConnectionLifeTime" },
				defaultValue: 0));

			AddOption(ConnectionReset = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "Connection Reset", "ConnectionReset" },
				defaultValue: true));

			AddOption(ConnectionIdlePingTime = new MySqlConnectionStringValueOption<uint>(
				keys: new[] { "Connection Idle Ping Time", "ConnectionIdlePingTime" },
				defaultValue: 0));

			AddOption(ConnectionIdleTimeout = new MySqlConnectionStringValueOption<uint>(
				keys: new[] { "Connection Idle Timeout", "ConnectionIdleTimeout" },
				defaultValue: 180));

			AddOption(MinimumPoolSize = new MySqlConnectionStringValueOption<uint>(
				keys: new[] { "Minimum Pool Size", "Min Pool Size", "MinimumPoolSize", "minpoolsize" },
				defaultValue: 0));

			AddOption(MaximumPoolSize = new MySqlConnectionStringValueOption<uint>(
				keys: new[] { "Maximum Pool Size", "Max Pool Size", "MaximumPoolSize", "maxpoolsize" },
				defaultValue: 100));

			// Other Options
			AddOption(AllowLoadLocalInfile = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "AllowLoadLocalInfile", "Allow Load Local Infile" },
				defaultValue: false));

			AddOption(AllowPublicKeyRetrieval = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "AllowPublicKeyRetrieval", "Allow Public Key Retrieval" },
				defaultValue: false));

			AddOption(AllowUserVariables = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "AllowUserVariables", "Allow User Variables" },
				defaultValue: false));

			AddOption(AllowZeroDateTime = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "AllowZeroDateTime", "Allow Zero DateTime" },
				defaultValue: false));

			AddOption(ApplicationName = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "ApplicationName", "Application Name" },
				defaultValue: null));

			AddOption(AutoEnlist = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "AutoEnlist", "Auto Enlist" },
				defaultValue: true));

			AddOption(CharacterSet = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "CharSet", "Character Set", "CharacterSet" },
				defaultValue: ""));

			AddOption(ConnectionTimeout = new MySqlConnectionStringValueOption<uint>(
				keys: new[] { "Connection Timeout", "ConnectionTimeout", "Connect Timeout" },
				defaultValue: 15u));

			AddOption(ConvertZeroDateTime = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "Convert Zero Datetime", "ConvertZeroDateTime" },
				defaultValue: false));

			AddOption(DateTimeKind = new MySqlConnectionStringValueOption<MySqlDateTimeKind>(
				keys: new[] { "DateTimeKind" },
				defaultValue: MySqlDateTimeKind.Unspecified));

			AddOption(DefaultCommandTimeout = new MySqlConnectionStringValueOption<uint>(
				keys: new[] { "Default Command Timeout", "DefaultCommandTimeout", "Command Timeout" },
				defaultValue: 30u));

			AddOption(ForceSynchronous = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "ForceSynchronous" },
				defaultValue: false));

			AddOption(GuidFormat = new MySqlConnectionStringValueOption<MySqlGuidFormat>(
				keys: new[] { "GuidFormat" },
				defaultValue: MySqlGuidFormat.Default));

			AddOption(IgnoreCommandTransaction = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "IgnoreCommandTransaction", "Ignore Command Transaction" },
				defaultValue: false));

			AddOption(IgnorePrepare = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "IgnorePrepare", "Ignore Prepare" },
				defaultValue: true));

			AddOption(InteractiveSession = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "InteractiveSession", "Interactive", "Interactive Session" },
				defaultValue: false));

			AddOption(Keepalive = new MySqlConnectionStringValueOption<uint>(
				keys: new[] { "Keep Alive", "Keepalive" },
				defaultValue: 0u));

			AddOption(NoBackslashEscapes = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "No Backslash Escapes", "NoBackslashEscapes" },
				defaultValue: false));

			AddOption(OldGuids = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "Old Guids", "OldGuids" },
				defaultValue: false));

			AddOption(PersistSecurityInfo = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "Persist Security Info", "PersistSecurityInfo" },
				defaultValue: false));

			AddOption(ServerRsaPublicKeyFile = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "ServerRSAPublicKeyFile", "Server RSA Public Key File" },
				defaultValue: null));

			AddOption(ServerSPN = new MySqlConnectionStringReferenceOption<string>(
				keys: new[] { "Server SPN", "ServerSPN" },
				defaultValue: null));

			AddOption(TreatTinyAsBoolean = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "Treat Tiny As Boolean", "TreatTinyAsBoolean" },
				defaultValue: true));

			AddOption(UseAffectedRows = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "Use Affected Rows", "UseAffectedRows" },
				defaultValue: false));

			AddOption(UseCompression = new MySqlConnectionStringValueOption<bool>(
				keys: new[] { "Compress", "Use Compression", "UseCompression" },
				defaultValue: false));

			AddOption(UseXaTransactions = new MySqlConnectionStringValueOption<bool>(
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

			if ((typeof(T) == typeof(MySqlLoadBalance) || typeof(T) == typeof(MySqlSslMode) || typeof(T) == typeof(MySqlDateTimeKind) || typeof(T) == typeof(MySqlGuidFormat) || typeof(T) == typeof(MySqlConnectionProtocol) || typeof(T) == typeof(MySqlCertificateStoreLocation)) && objectValue is string enumString)
			{
				try
				{
					return (T) Enum.Parse(typeof(T), enumString, ignoreCase: true);
				}
				catch (Exception ex) when (!(ex is ArgumentException))
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

	internal sealed class MySqlConnectionStringNonNullOption<T> : MySqlConnectionStringOption
		where T : class
	{
		public MySqlConnectionStringNonNullOption(IReadOnlyList<string> keys, T defaultValue, Func<T?, T>? coerce = null)
			: base(keys)
		{
			m_defaultValue = defaultValue;
			m_coerce = coerce;
		}

		public T GetValue(MySqlConnectionStringBuilder builder) =>
			builder.TryGetValue(Key, out var objectValue) ? ChangeType(objectValue) : m_defaultValue;

		public void SetValue(MySqlConnectionStringBuilder builder, T? value) =>
			builder.DoSetValue(Key, m_coerce is null ? value : m_coerce(value));

		public override object? GetObject(MySqlConnectionStringBuilder builder) => GetValue(builder);

		public override void SetObject(MySqlConnectionStringBuilder builder, object value) => SetValue(builder, ChangeType(value));

		private static T ChangeType(object objectValue) =>
			(T) Convert.ChangeType(objectValue, typeof(T), CultureInfo.InvariantCulture);

		readonly T m_defaultValue;
		readonly Func<T?, T>? m_coerce;
	}

	internal sealed class MySqlConnectionStringReferenceOption<T> : MySqlConnectionStringOption
		where T : class
	{
		public MySqlConnectionStringReferenceOption(IReadOnlyList<string> keys, T? defaultValue, Func<T?, T?>? coerce = null)
			: base(keys)
		{
			m_defaultValue = defaultValue;
			m_coerce = coerce;
		}

		public T? GetValue(MySqlConnectionStringBuilder builder) =>
			builder.TryGetValue(Key, out var objectValue) ? ChangeType(objectValue) : m_defaultValue;

		public void SetValue(MySqlConnectionStringBuilder builder, T? value) =>
			builder.DoSetValue(Key, m_coerce is null ? value : m_coerce(value));

		public override object? GetObject(MySqlConnectionStringBuilder builder) => GetValue(builder);

		public override void SetObject(MySqlConnectionStringBuilder builder, object value) => SetValue(builder, ChangeType(value));

		private static T ChangeType(object objectValue) =>
			(T) Convert.ChangeType(objectValue, typeof(T), CultureInfo.InvariantCulture);

		readonly T? m_defaultValue;
		readonly Func<T?, T?>? m_coerce;
	}
}
