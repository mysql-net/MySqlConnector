using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;

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

		public string Server
		{
			get { return MySqlConnectionStringOption.Server.GetValue(this); }
			set { MySqlConnectionStringOption.Server.SetValue(this, value); }
		}

		public string UserID
		{
			get { return MySqlConnectionStringOption.UserID.GetValue(this); }
			set { MySqlConnectionStringOption.UserID.SetValue(this, value); }
		}

		public string Password
		{
			get { return MySqlConnectionStringOption.Password.GetValue(this); }
			set { MySqlConnectionStringOption.Password.SetValue(this, value); }
		}

		public string Database
		{
			get { return MySqlConnectionStringOption.Database.GetValue(this); }
			set { MySqlConnectionStringOption.Database.SetValue(this, value); }
		}

		public uint Port
		{
			get { return MySqlConnectionStringOption.Port.GetValue(this); }
			set { MySqlConnectionStringOption.Port.SetValue(this, value); }
		}

		public bool AllowUserVariables
		{
			get { return MySqlConnectionStringOption.AllowUserVariables.GetValue(this); }
			set { MySqlConnectionStringOption.AllowUserVariables.SetValue(this, value); }
		}

		public string CharacterSet
		{
			get { return MySqlConnectionStringOption.CharacterSet.GetValue(this); }
			set { MySqlConnectionStringOption.CharacterSet.SetValue(this, value); }
		}

		public bool ConvertZeroDateTime
		{
			get { return MySqlConnectionStringOption.ConvertZeroDateTime.GetValue(this); }
			set { MySqlConnectionStringOption.ConvertZeroDateTime.SetValue(this, value); }
		}

		public bool OldGuids
		{
			get { return MySqlConnectionStringOption.OldGuids.GetValue(this); }
			set { MySqlConnectionStringOption.OldGuids.SetValue(this, value); }
		}

		public bool PersistSecurityInfo
		{
			get { return MySqlConnectionStringOption.PersistSecurityInfo.GetValue(this); }
			set { MySqlConnectionStringOption.PersistSecurityInfo.SetValue(this, value); }
		}

		public bool UseCompression
		{
			get { return MySqlConnectionStringOption.UseCompression.GetValue(this); }
			set { MySqlConnectionStringOption.UseCompression.SetValue(this, value); }
		}

		public MySqlSslMode SslMode
		{
			get { return MySqlConnectionStringOption.SslMode.GetValue(this); }
			set { MySqlConnectionStringOption.SslMode.SetValue(this, value); }
		}

		public string CertificateFile
		{
			get { return MySqlConnectionStringOption.CertificateFile.GetValue(this); }
			set { MySqlConnectionStringOption.CertificateFile.SetValue(this, value); }
		}

		public string CertificatePassword
		{
			get { return MySqlConnectionStringOption.CertificatePassword.GetValue(this); }
			set { MySqlConnectionStringOption.CertificatePassword.SetValue(this, value); }
		}

		public bool Pooling
		{
			get { return MySqlConnectionStringOption.Pooling.GetValue(this); }
			set { MySqlConnectionStringOption.Pooling.SetValue(this, value); }
		}

		public bool ConnectionReset
		{
			get { return MySqlConnectionStringOption.ConnectionReset.GetValue(this); }
			set { MySqlConnectionStringOption.ConnectionReset.SetValue(this, value); }
		}

		public uint ConnectionTimeout
		{
			get { return MySqlConnectionStringOption.ConnectionTimeout.GetValue(this); }
			set { MySqlConnectionStringOption.ConnectionTimeout.SetValue(this, value); }
		}

		public uint MinimumPoolSize
		{
			get { return MySqlConnectionStringOption.MinimumPoolSize.GetValue(this); }
			set { MySqlConnectionStringOption.MinimumPoolSize.SetValue(this, value); }
		}

		public uint MaximumPoolSize
		{
			get { return MySqlConnectionStringOption.MaximumPoolSize.GetValue(this); }
			set { MySqlConnectionStringOption.MaximumPoolSize.SetValue(this, value); }
		}

		public bool UseAffectedRows
		{
			get { return MySqlConnectionStringOption.UseAffectedRows.GetValue(this); }
			set { MySqlConnectionStringOption.UseAffectedRows.SetValue(this, value); }
		}

		public bool ForceSynchronous
		{
			get { return MySqlConnectionStringOption.ForceSynchronous.GetValue(this); }
			set { MySqlConnectionStringOption.ForceSynchronous.SetValue(this, value); }
		}

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
			get { return MySqlConnectionStringOption.GetOptionForKey(key).GetObject(this); }
			set { base[MySqlConnectionStringOption.GetOptionForKey(key).Key] = Convert.ToString(value, CultureInfo.InvariantCulture); }
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
		public static readonly MySqlConnectionStringOption<string> Server;
		public static readonly MySqlConnectionStringOption<string> UserID;
		public static readonly MySqlConnectionStringOption<string> Password;
		public static readonly MySqlConnectionStringOption<string> Database;
		public static readonly MySqlConnectionStringOption<uint> Port;
		public static readonly MySqlConnectionStringOption<bool> AllowUserVariables;
		public static readonly MySqlConnectionStringOption<string> CharacterSet;
		public static readonly MySqlConnectionStringOption<bool> ConvertZeroDateTime;
		public static readonly MySqlConnectionStringOption<bool> OldGuids;
		public static readonly MySqlConnectionStringOption<bool> PersistSecurityInfo;
		public static readonly MySqlConnectionStringOption<bool> UseCompression;
		public static readonly MySqlConnectionStringOption<MySqlSslMode> SslMode;
		public static readonly MySqlConnectionStringOption<string> CertificateFile;
		public static readonly MySqlConnectionStringOption<string> CertificatePassword;
		public static readonly MySqlConnectionStringOption<bool> Pooling;
		public static readonly MySqlConnectionStringOption<bool> ConnectionReset;
		public static readonly MySqlConnectionStringOption<uint> ConnectionTimeout;
		public static readonly MySqlConnectionStringOption<uint> MinimumPoolSize;
		public static readonly MySqlConnectionStringOption<uint> MaximumPoolSize;
		public static readonly MySqlConnectionStringOption<bool> UseAffectedRows;
		public static readonly MySqlConnectionStringOption<bool> ForceSynchronous;

		public static MySqlConnectionStringOption TryGetOptionForKey(string key)
		{
			MySqlConnectionStringOption option;
			return s_options.TryGetValue(key, out option) ? option : null;
		}

		public static MySqlConnectionStringOption GetOptionForKey(string key)
		{
			var option = TryGetOptionForKey(key);
			if (option == null)
				throw new InvalidOperationException("Option '{0}' not supported.".FormatInvariant(key));
			return option;
		}

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

			AddOption(Server = new MySqlConnectionStringOption<string>(
				keys: new[] { "Server", "Host", "Data Source", "DataSource", "Address", "Addr", "Network Address" },
				defaultValue: ""));

			AddOption(UserID = new MySqlConnectionStringOption<string>(
				keys: new[] { "User Id", "UserID", "Username", "Uid", "User name", "User" },
				defaultValue: ""));

			AddOption(Password = new MySqlConnectionStringOption<string>(
				keys: new[] { "Password", "pwd" },
				defaultValue: ""));

			AddOption(Database = new MySqlConnectionStringOption<string>(
				keys: new[] { "Database", "Initial Catalog" },
				defaultValue: ""));

			AddOption(Port = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Port" },
				defaultValue: 3306u));

			AddOption(AllowUserVariables = new MySqlConnectionStringOption<bool>(
				keys: new[] { "AllowUserVariables", "Allow User Variables" },
				defaultValue: false));

			AddOption(CharacterSet = new MySqlConnectionStringOption<string>(
				keys: new[] { "CharSet", "Character Set", "CharacterSet" },
				defaultValue: ""));

			AddOption(ConvertZeroDateTime = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Convert Zero Datetime", "ConvertZeroDateTime" },
				defaultValue: false));

			AddOption(OldGuids = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Old Guids", "OldGuids" },
				defaultValue: false));

			AddOption(PersistSecurityInfo = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Persist Security Info", "PersistSecurityInfo" },
				defaultValue: false));

			AddOption(UseCompression = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Compress", "Use Compression", "UseCompression" },
				defaultValue: false));

			AddOption(CertificateFile = new MySqlConnectionStringOption<string>(
				keys: new[] { "CertificateFile", "Certificate File" },
				defaultValue: null));

			AddOption(CertificatePassword = new MySqlConnectionStringOption<string>(
				keys: new[] { "CertificatePassword", "Certificate Password" },
				defaultValue: null));

			AddOption(SslMode = new MySqlConnectionStringOption<MySqlSslMode>(
				keys: new[] { "SSL Mode", "SslMode" },
				defaultValue: MySqlSslMode.None));

			AddOption(Pooling = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Pooling" },
				defaultValue: true));

			AddOption(ConnectionReset = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Connection Reset", "ConnectionReset" },
				defaultValue: true));

			AddOption(ConnectionTimeout = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Connection Timeout", "ConnectionTimeout", "Connect Timeout" },
				defaultValue: 15u));

			AddOption(MinimumPoolSize = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Minimum Pool Size", "Min Pool Size", "MinimumPoolSize", "minpoolsize" },
				defaultValue: 0));

			AddOption(MaximumPoolSize = new MySqlConnectionStringOption<uint>(
				keys: new[] { "Maximum Pool Size", "Max Pool Size", "MaximumPoolSize", "maxpoolsize" },
				defaultValue: 100));

			AddOption(UseAffectedRows = new MySqlConnectionStringOption<bool>(
				keys: new[] { "Use Affected Rows", "UseAffectedRows" },
				defaultValue: true));

			AddOption(ForceSynchronous = new MySqlConnectionStringOption<bool>(
				keys: new[] { "ForceSynchronous" },
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

		public T GetValue(MySqlConnectionStringBuilder builder)
		{
			object objectValue;
			return builder.TryGetValue(Key, out objectValue) ? ChangeType(objectValue) : m_defaultValue;
		}

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
			if (typeof(T) == typeof(bool) && objectValue is string)
			{
				if (string.Equals((string) objectValue, "yes", StringComparison.OrdinalIgnoreCase))
					return (T) (object) true;
				if (string.Equals((string) objectValue, "no", StringComparison.OrdinalIgnoreCase))
					return (T) (object) false;
			}

			if (typeof(T) == typeof(MySqlSslMode) && objectValue is string)
			{
				foreach (var val in Enum.GetValues(typeof(T)))
				{
					if (string.Equals((string) objectValue, val.ToString(), StringComparison.OrdinalIgnoreCase))
						return (T) val;
				}
				throw new InvalidOperationException("Value '{0}' not supported for option '{1}'.".FormatInvariant(objectValue, typeof(T).Name));
			}

			return (T) Convert.ChangeType(objectValue, typeof(T), CultureInfo.InvariantCulture);
		}

		readonly T m_defaultValue;
		readonly Func<T, T> m_coerce;
	}
}
