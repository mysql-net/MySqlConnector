using System;
using System.Collections.Generic;
using System.IO;
using MySql.Data.MySqlClient;

namespace MySql.Data.Serialization
{
	internal sealed class ConnectionSettings
	{
		public ConnectionSettings(MySqlConnectionStringBuilder csb)
		{
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
				Hostnames = csb.Server.Split(',');
				Port = (int) csb.Port;
			}
			UserID = csb.UserID;
			Password = csb.Password;
			Database = csb.Database;

			// SSL/TLS Options
			SslMode = csb.SslMode;
			CertificateFile = csb.CertificateFile;
			CertificatePassword = csb.CertificatePassword;

			// Connection Pooling Options
			Pooling = csb.Pooling;
			ConnectionLifeTime = (int)csb.ConnectionLifeTime;
			ConnectionReset = csb.ConnectionReset;
			ConnectionIdleTimeout = (int)csb.ConnectionIdleTimeout;
			if (csb.MinimumPoolSize > csb.MaximumPoolSize)
				throw new MySqlException("MaximumPoolSize must be greater than or equal to MinimumPoolSize");
			MinimumPoolSize = (int)csb.MinimumPoolSize;
			MaximumPoolSize = (int)csb.MaximumPoolSize;

			// Other Options
			AllowUserVariables = csb.AllowUserVariables;
			AutoEnlist = csb.AutoEnlist;
			BufferResultSets = csb.BufferResultSets;
			ConnectionTimeout = (int)csb.ConnectionTimeout;
			ConvertZeroDateTime = csb.ConvertZeroDateTime;
			ForceSynchronous = csb.ForceSynchronous;
			Keepalive = csb.Keepalive;
			OldGuids = csb.OldGuids;
			PersistSecurityInfo = csb.PersistSecurityInfo;
			TreatTinyAsBoolean = csb.TreatTinyAsBoolean;
			UseAffectedRows = csb.UseAffectedRows;
			UseCompression = csb.UseCompression;
		}

		// Base Options
		public string ConnectionString { get; }
		public ConnectionType ConnectionType { get; }
		public IEnumerable<string> Hostnames { get; }
		public int Port { get; }
		public string UnixSocket { get; }
		public string UserID { get; }
		public string Password { get; }
		public string Database { get; }

		// SSL/TLS Options
		public MySqlSslMode SslMode { get; }
		public string CertificateFile { get; }
		public string CertificatePassword { get; }

		// Connection Pooling Options
		public bool Pooling { get; }
		public int ConnectionLifeTime { get; }
		public bool ConnectionReset { get; }
		public int ConnectionIdleTimeout { get; }
		public int MinimumPoolSize { get; }
		public int MaximumPoolSize { get; }

		// Other Options
		public bool AllowUserVariables { get; }
		public bool AutoEnlist { get; }
		public bool BufferResultSets { get; }
		public int ConnectionTimeout { get; }
		public bool ConvertZeroDateTime { get; }
		public bool ForceSynchronous { get; }
		public uint Keepalive { get; }
		public bool OldGuids { get; }
		public bool PersistSecurityInfo { get; }
		public bool TreatTinyAsBoolean { get; }
		public bool UseAffectedRows { get; }
		public bool UseCompression { get; }

		// Helper Functions
		private int? _connectionTimeoutMilliseconds;
		public int ConnectionTimeoutMilliseconds
		{
			get
			{
				if (!_connectionTimeoutMilliseconds.HasValue)
				{
					try
					{
						checked
						{
							_connectionTimeoutMilliseconds = ConnectionTimeout * 1000;
						}
					}
					catch (OverflowException)
					{
						_connectionTimeoutMilliseconds = Int32.MaxValue;
					}
				}
				return _connectionTimeoutMilliseconds.Value;
			}
		}

	}
}
