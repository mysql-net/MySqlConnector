using System;
using System.Collections.Generic;
using System.IO;
using MySql.Data.MySqlClient;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class ConnectionSettings
	{
		public ConnectionSettings(MySqlConnectionStringBuilder csb)
		{
			ConnectionStringBuilder = csb;
			ConnectionString = csb.ConnectionString;

			// Base Options
			if (!Utility.IsWindows() && (csb.Server.StartsWith("/", StringComparison.Ordinal) || csb.Server.StartsWith("./", StringComparison.Ordinal)))
			{
				if (!File.Exists(csb.Server))
					throw new MySqlException("Cannot find Unix Socket at " + csb.Server);
				ConnectionType = ConnectionType.Unix;
				UnixSocket = Path.GetFullPath(csb.Server);
			}
			else
			{
				ConnectionType = ConnectionType.Tcp;
				HostNames = csb.Server.Split(',');
				LoadBalance = csb.LoadBalance;
				Port = (int) csb.Port;
			}
			UserID = csb.UserID;
			Password = csb.Password;
			Database = csb.Database;

			// SSL/TLS Options
			SslMode = csb.SslMode;
			CertificateFile = csb.CertificateFile;
			CertificatePassword = csb.CertificatePassword;
			CACertificateFile = csb.CACertificateFile;

			// Connection Pooling Options
			Pooling = csb.Pooling;
			ConnectionLifeTime = Math.Min(csb.ConnectionLifeTime, uint.MaxValue / 1000) * 1000;
			ConnectionReset = csb.ConnectionReset;
			ConnectionIdlePingTime = Math.Min(csb.ConnectionIdlePingTime, uint.MaxValue / 1000) * 1000;
			ConnectionIdleTimeout = (int)csb.ConnectionIdleTimeout;
			if (csb.MinimumPoolSize > csb.MaximumPoolSize)
				throw new MySqlException("MaximumPoolSize must be greater than or equal to MinimumPoolSize");
			MinimumPoolSize = (int)csb.MinimumPoolSize;
			MaximumPoolSize = (int)csb.MaximumPoolSize;

			// Other Options
			AllowPublicKeyRetrieval = csb.AllowPublicKeyRetrieval;
			AllowUserVariables = csb.AllowUserVariables;
			AutoEnlist = csb.AutoEnlist;
			ConnectionTimeout = (int)csb.ConnectionTimeout;
			ConvertZeroDateTime = csb.ConvertZeroDateTime;
			DefaultCommandTimeout = (int) csb.DefaultCommandTimeout;
			ForceSynchronous = csb.ForceSynchronous;
			IgnoreCommandTransaction = csb.IgnoreCommandTransaction;
			Keepalive = csb.Keepalive;
			OldGuids = csb.OldGuids;
			PersistSecurityInfo = csb.PersistSecurityInfo;
			ServerRsaPublicKeyFile = csb.ServerRsaPublicKeyFile;
			TreatTinyAsBoolean = csb.TreatTinyAsBoolean;
			UseAffectedRows = csb.UseAffectedRows;
			UseCompression = csb.UseCompression;
		}

		/// <summary>
		/// The <see cref="MySqlConnectionStringBuilder" /> that was used to create this <see cref="ConnectionSettings" />.!--
		/// This object must not be mutated.
		/// </summary>
		public MySqlConnectionStringBuilder ConnectionStringBuilder { get; }

		// Base Options
		public string ConnectionString { get; }
		public ConnectionType ConnectionType { get; }
		public IReadOnlyList<string> HostNames { get; }
		public MySqlLoadBalance LoadBalance { get; }
		public int Port { get; }
		public string UnixSocket { get; }
		public string UserID { get; }
		public string Password { get; }
		public string Database { get; }

		// SSL/TLS Options
		public MySqlSslMode SslMode { get; }
		public string CertificateFile { get; }
		public string CertificatePassword { get; }
		public string CACertificateFile { get; }

		// Connection Pooling Options
		public bool Pooling { get; }
		public uint ConnectionLifeTime { get; }
		public bool ConnectionReset { get; }
		public uint ConnectionIdlePingTime { get; }
		public int ConnectionIdleTimeout { get; }
		public int MinimumPoolSize { get; }
		public int MaximumPoolSize { get; }

		// Other Options
		public bool AllowPublicKeyRetrieval { get; }
		public bool AllowUserVariables { get; }
		public bool AutoEnlist { get; }
		public int ConnectionTimeout { get; }
		public bool ConvertZeroDateTime { get; }
		public int DefaultCommandTimeout { get; }
		public bool ForceSynchronous { get; }
		public bool IgnoreCommandTransaction { get; }
		public uint Keepalive { get; }
		public bool OldGuids { get; }
		public bool PersistSecurityInfo { get; }
		public string ServerRsaPublicKeyFile { get; }
		public bool TreatTinyAsBoolean { get; }
		public bool UseAffectedRows { get; }
		public bool UseCompression { get; }

		// Helper Functions
		int? m_connectionTimeoutMilliseconds;
		public int ConnectionTimeoutMilliseconds
		{
			get
			{
				if (!m_connectionTimeoutMilliseconds.HasValue)
				{
					try
					{
						checked
						{
							m_connectionTimeoutMilliseconds = ConnectionTimeout * 1000;
						}
					}
					catch (OverflowException)
					{
						m_connectionTimeoutMilliseconds = Int32.MaxValue;
					}
				}
				return m_connectionTimeoutMilliseconds.Value;
			}
		}
	}
}
