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
		internal readonly string ConnectionString;
		internal readonly ConnectionType ConnectionType;
		internal readonly IEnumerable<string> Hostnames;
		internal readonly int Port;
		internal readonly string UnixSocket;
		internal readonly string UserID;
		internal readonly string Password;
		internal readonly string Database;

		// SSL/TLS Options
		internal readonly MySqlSslMode SslMode;
		internal readonly string CertificateFile;
		internal readonly string CertificatePassword;

		// Connection Pooling Options
		internal readonly bool Pooling;
		internal readonly int ConnectionLifeTime;
		internal readonly bool ConnectionReset;
		internal readonly int ConnectionIdleTimeout;
		internal readonly int MinimumPoolSize;
		internal readonly int MaximumPoolSize;

		// Other Options
		internal readonly bool AllowUserVariables;
		internal readonly bool AutoEnlist;
		internal readonly bool BufferResultSets;
		internal readonly int ConnectionTimeout;
		internal readonly bool ConvertZeroDateTime;
		internal readonly bool ForceSynchronous;
		internal readonly uint Keepalive;
		internal readonly bool OldGuids;
		internal readonly bool PersistSecurityInfo;
		internal readonly bool TreatTinyAsBoolean;
		internal readonly bool UseAffectedRows;
		internal readonly bool UseCompression;
	}

}
