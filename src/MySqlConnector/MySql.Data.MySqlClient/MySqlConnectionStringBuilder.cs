using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Security.Authentication;
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

		// SSL/TLS Options
		public MySqlSslMode SslMode
		{
			get => MySqlConnectionStringOption.SslMode.GetValue(this);
			set => MySqlConnectionStringOption.SslMode.SetValue(this, value);
		}

		public SslProtocols SslProtocols
		{
			get => MySqlConnectionStringOption.SslProtocols.GetValue(this);
			set => MySqlConnectionStringOption.SslProtocols.SetValue(this, value);
		}

		public string CertificateFile
		{
			get => MySqlConnectionStringOption.CertificateFile.GetValue(this);
			set => MySqlConnectionStringOption.CertificateFile.SetValue(this, value);
		}

		public string CertificatePassword
		{
			get => MySqlConnectionStringOption.CertificatePassword.GetValue(this);
			set => MySqlConnectionStringOption.CertificatePassword.SetValue(this, value);
		}

		public string CACertificateFile
		{
			get => MySqlConnectionStringOption.CACertificateFile.GetValue(this);
			set => MySqlConnectionStringOption.CACertificateFile.SetValue(this, value);
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

		public bool AutoEnlist
		{
			get => MySqlConnectionStringOption.AutoEnlist.GetValue(this);
			set => MySqlConnectionStringOption.AutoEnlist.SetValue(this, value);
		}

		public string CharacterSet
		{
			get => MySqlConnectionStringOption.CharacterSet.GetValue(this);
			set => MySqlConnectionStringOption.CharacterSet.SetValue(this, value);
		}

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

		public uint Keepalive
		{
			get => MySqlConnectionStringOption.Keepalive.GetValue(this);
			set => MySqlConnectionStringOption.Keepalive.SetValue(this, value);
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

		public string ServerRsaPublicKeyFile
		{
			get => MySqlConnectionStringOption.ServerRsaPublicKeyFile.GetValue(this);
			set => MySqlConnectionStringOption.ServerRsaPublicKeyFile.SetValue(this, value);
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

		// Other Methods
		public override bool ContainsKey(string key)
		{
			var option = MySqlConnectionStringOption.TryGetOptionForKey(key);
			return option != null && base.ContainsKey(option.Key);
		}

		public override bool Remove(string key)
		{
			var option = MySqlConnectionStringOption.TryGetOptionForKey(key);
			return option != null && base.Remove(option.Key);
		}

		public override object this[string key]
		{
			get => MySqlConnectionStringOption.GetOptionForKey(key).GetObject(this);
			set => base[MySqlConnectionStringOption.GetOptionForKey(key).Key] = Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		internal string GetConnectionString(bool includePassword)
		{
			var connectionString = ConnectionString;
			if (includePassword)
				return connectionString;

			if (m_cachedConnectionString != connectionString)
			{
				var csb = new MySqlConnectionStringBuilder(connectionString);
				foreach (string key in Keys)
					foreach (var passwordKey in MySqlConnectionStringOption.Password.Keys)
						if (string.Equals(key, passwordKey, StringComparison.OrdinalIgnoreCase))
							csb.Remove(key);
				m_cachedConnectionStringWithoutPassword = csb.ConnectionString;
				m_cachedConnectionString = connectionString;
			}

			return m_cachedConnectionStringWithoutPassword;
		}

		string m_cachedConnectionString;
		string m_cachedConnectionStringWithoutPassword;
	}

	internal abstract class MySqlConnectionStringOption
	{
		// Base Options
		public static readonly MySqlConnectionStringOption<string> Server;
		public static readonly MySqlConnectionStringOption<uint> Port;
		public static readonly MySqlConnectionStringOption<string> UserID;
		public static readonly MySqlConnectionStringOption<string> Password;
		public static readonly MySqlConnectionStringOption<string> Database;
		public static readonly MySqlConnectionStringOption<MySqlLoadBalance> LoadBalance;

		// SSL/TLS Options
		public static readonly MySqlConnectionStringOption<MySqlSslMode> SslMode;
		public static readonly MySqlConnectionStringOption<SslProtocols> SslProtocols;
		public static readonly MySqlConnectionStringOption<string> CertificateFile;
		public static readonly MySqlConnectionStringOption<string> CertificatePassword;
		public static readonly MySqlConnectionStringOption<string> CACertificateFile;

		// Connection Pooling Options
		public static readonly MySqlConnectionStringOption<bool> Pooling;
		public static readonly MySqlConnectionStringOption<uint> ConnectionLifeTime;
		public static readonly MySqlConnectionStringOption<bool> ConnectionReset;
		public static readonly MySqlConnectionStringOption<uint> ConnectionIdleTimeout;
		public static readonly MySqlConnectionStringOption<uint> MinimumPoolSize;
		public static readonly MySqlConnectionStringOption<uint> MaximumPoolSize;

		// Other Options
		public static readonly MySqlConnectionStringOption<bool> AllowPublicKeyRetrieval;
		public static readonly MySqlConnectionStringOption<bool> AllowUserVariables;
		public static readonly MySqlConnectionStringOption<bool> AutoEnlist;
		public static readonly MySqlConnectionStringOption<string> CharacterSet;
		public static readonly MySqlConnectionStringOption<uint> ConnectionTimeout;
		public static readonly MySqlConnectionStringOption<bool> ConvertZeroDateTime;
		public static readonly MySqlConnectionStringOption<uint> DefaultCommandTimeout;
		public static readonly MySqlConnectionStringOption<bool> ForceSynchronous;
		public static readonly MySqlConnectionStringOption<uint> Keepalive;
		public static readonly MySqlConnectionStringOption<bool> OldGuids;
		public static readonly MySqlConnectionStringOption<bool> PersistSecurityInfo;
		public static readonly MySqlConnectionStringOption<string> ServerRsaPublicKeyFile;
		public static readonly MySqlConnectionStringOption<bool> TreatTinyAsBoolean;
		public static readonly MySqlConnectionStringOption<bool> UseAffectedRows;
		public static readonly MySqlConnectionStringOption<bool> UseCompression;

		public static MySqlConnectionStringOption TryGetOptionForKey(string key) =>
			s_options.TryGetValue(key, out var option) ? option : null;

		public static MySqlConnectionStringOption GetOptionForKey(string key) =>
			TryGetOptionForKey(key) ?? throw new ArgumentException("Option '{0}' not supported.".FormatInvariant(key));

		public string Key => m_keys[0];
		public IReadOnlyList<string> Keys => m_keys;

		public abstract object GetObject(MySqlConnectionStringBuilder builder);

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
			AddOption(Server = new MySqlConnectionStringOption<string>(
				keys: new[] { "Server", "Host", "Data Source", "DataSource", "Address", "Addr", "Network Address" },
				defaultValue: ""));

			AddOption(Port = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Port" },
				defaultValue: 3306u));

			AddOption(UserID = new MySqlConnectionStringOption<string>(
				keys: new[] { "User Id", "UserID", "Username", "Uid", "User name", "User" },
				defaultValue: ""));

			AddOption(Password = new MySqlConnectionStringOption<string>(
				keys: new[] { "Password", "pwd" },
				defaultValue: ""));

			AddOption(Database = new MySqlConnectionStringOption<string>(
				keys: new[] { "Database", "Initial Catalog" },
				defaultValue: ""));

			AddOption(LoadBalance = new MySqlConnectionStringOption<MySqlLoadBalance>(
				keys: new[] { "LoadBalance", "Load Balance" },
				defaultValue: MySqlLoadBalance.RoundRobin));

			// SSL/TLS Options
			AddOption(SslMode = new MySqlConnectionStringOption<MySqlSslMode>(
				keys: new[] { "SSL Mode", "SslMode" },
				defaultValue: MySqlSslMode.Preferred));

			var sslProtocolsDefault = System.Security.Authentication.SslProtocols.Tls |
				 System.Security.Authentication.SslProtocols.Tls11;
			if (!Utility.IsWindows())
			{
				sslProtocolsDefault |= System.Security.Authentication.SslProtocols.Tls12;
			}

			AddOption(SslProtocols = new MySqlConnectionStringOption<SslProtocols>(
				keys: new[] { "SSL Protocols", "SslProtocols" },
				defaultValue: sslProtocolsDefault));

			AddOption(CertificateFile = new MySqlConnectionStringOption<string>(
				keys: new[] { "CertificateFile", "Certificate File" },
				defaultValue: null));

			AddOption(CertificatePassword = new MySqlConnectionStringOption<string>(
				keys: new[] { "CertificatePassword", "Certificate Password" },
				defaultValue: null));

			AddOption(CACertificateFile = new MySqlConnectionStringOption<string>(
				keys: new[] { "CACertificateFile", "CA Certificate File" },
				defaultValue: null));

			// Connection Pooling Options
			AddOption(Pooling = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Pooling" },
				defaultValue: true));

			AddOption(ConnectionLifeTime = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Connection Lifetime", "ConnectionLifeTime" },
				defaultValue: 0));

			AddOption(ConnectionReset = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Connection Reset", "ConnectionReset" },
				defaultValue: true));

			AddOption(ConnectionIdleTimeout = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Connection Idle Timeout", "ConnectionIdleTimeout" },
				defaultValue: 180));

			AddOption(MinimumPoolSize = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Minimum Pool Size", "Min Pool Size", "MinimumPoolSize", "minpoolsize" },
				defaultValue: 0));

			AddOption(MaximumPoolSize = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Maximum Pool Size", "Max Pool Size", "MaximumPoolSize", "maxpoolsize" },
				defaultValue: 100));

			// Other Options
			AddOption(AllowPublicKeyRetrieval = new MySqlConnectionStringOption<bool>(
				keys: new[] { "AllowPublicKeyRetrieval", "Allow Public Key Retrieval" },
				defaultValue: false));

			AddOption(AllowUserVariables = new MySqlConnectionStringOption<bool>(
				keys: new[] { "AllowUserVariables", "Allow User Variables" },
				defaultValue: false));

			AddOption(AutoEnlist = new MySqlConnectionStringOption<bool>(
				keys: new[] { "AutoEnlist", "Auto Enlist" },
				defaultValue: true));

			AddOption(CharacterSet = new MySqlConnectionStringOption<string>(
				keys: new[] { "CharSet", "Character Set", "CharacterSet" },
				defaultValue: ""));

			AddOption(ConnectionTimeout = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Connection Timeout", "ConnectionTimeout", "Connect Timeout" },
				defaultValue: 15u));

			AddOption(ConvertZeroDateTime = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Convert Zero Datetime", "ConvertZeroDateTime" },
				defaultValue: false));

			AddOption(DefaultCommandTimeout = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Default Command Timeout", "DefaultCommandTimeout", "Command Timeout" },
				defaultValue: 30u));

			AddOption(ForceSynchronous = new MySqlConnectionStringOption<bool>(
				keys: new[] { "ForceSynchronous" },
				defaultValue: false));

			AddOption(Keepalive = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Keep Alive", "Keepalive" },
				defaultValue: 0u));

			AddOption(OldGuids = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Old Guids", "OldGuids" },
				defaultValue: false));

			AddOption(PersistSecurityInfo = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Persist Security Info", "PersistSecurityInfo" },
				defaultValue: false));

			AddOption(ServerRsaPublicKeyFile = new MySqlConnectionStringOption<string>(
				keys: new[] { "ServerRSAPublicKeyFile", "Server RSA Public Key File" },
				defaultValue: null));

			AddOption(TreatTinyAsBoolean = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Treat Tiny As Boolean", "TreatTinyAsBoolean" },
				defaultValue: true));

			AddOption(UseAffectedRows = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Use Affected Rows", "UseAffectedRows" },
				defaultValue: true));

			AddOption(UseCompression = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Compress", "Use Compression", "UseCompression" },
				defaultValue: false));
		}

		static readonly Dictionary<string, MySqlConnectionStringOption> s_options;

		readonly IReadOnlyList<string> m_keys;
	}

	internal sealed class MySqlConnectionStringOption<T> : MySqlConnectionStringOption
	{
		public MySqlConnectionStringOption(IReadOnlyList<string> keys, T defaultValue, Func<T, T> coerce = null)
			: base(keys)
		{
			m_defaultValue = defaultValue;
			m_coerce = coerce;
		}

		public T GetValue(MySqlConnectionStringBuilder builder) =>
			builder.TryGetValue(Key, out var objectValue) ? ChangeType(objectValue) : m_defaultValue;

		public void SetValue(MySqlConnectionStringBuilder builder, T value)
		{
			builder[Key] = m_coerce != null ? m_coerce(value) : value;
		}

		public override object GetObject(MySqlConnectionStringBuilder builder)
		{
			return GetValue(builder);
		}

		private static T ChangeType(object objectValue)
		{
			if (typeof(T) == typeof(bool) && objectValue is string booleanString)
			{
				if (string.Equals(booleanString, "yes", StringComparison.OrdinalIgnoreCase))
					return (T) (object) true;
				if (string.Equals(booleanString, "no", StringComparison.OrdinalIgnoreCase))
					return (T) (object) false;
			}

			if (typeof(T) == typeof(MySqlLoadBalance) && objectValue is string loadBalanceString)
			{
				foreach (var val in Enum.GetValues(typeof(T)))
				{
					if (string.Equals(loadBalanceString, val.ToString(), StringComparison.OrdinalIgnoreCase))
						return (T) val;
				}
				throw new InvalidOperationException("Value '{0}' not supported for option '{1}'.".FormatInvariant(objectValue, typeof(T).Name));
			}

			if (typeof(T) == typeof(MySqlSslMode) && objectValue is string sslModeString)
			{
				foreach (var val in Enum.GetValues(typeof(T)))
				{
					if (string.Equals(sslModeString, val.ToString(), StringComparison.OrdinalIgnoreCase))
						return (T) val;
				}
				throw new InvalidOperationException("Value '{0}' not supported for option '{1}'.".FormatInvariant(objectValue, typeof(T).Name));
			}

			if (typeof(T) == typeof(SslProtocols) && objectValue is string sslProtocolsString)
			{
				try
				{
					return (T) Enum.Parse(typeof(T), sslProtocolsString, true);
				}
				catch (Exception)
				{
					throw new InvalidOperationException("Value '{0}' not supported for option '{1}'.".FormatInvariant(objectValue, typeof(T).Name));
				}
			}

			return (T) Convert.ChangeType(objectValue, typeof(T), CultureInfo.InvariantCulture);
		}

		readonly T m_defaultValue;
		readonly Func<T, T> m_coerce;
	}
}
