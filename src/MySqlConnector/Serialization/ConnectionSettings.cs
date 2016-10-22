using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MySql.Data.MySqlClient;

namespace MySql.Data.Serialization
{
	internal sealed class ConnectionSettings
	{
		public ConnectionSettings(MySqlConnectionStringBuilder csb)
		{
			ConnectionString = csb.ConnectionString;

			// Base Options
			Hostnames = csb.Server.Split(',');
			Port = (int) csb.Port;
			UserID = csb.UserID;
			Password = csb.Password;
			Database = csb.Database;

			// SSL/TLS Options
			SslMode = csb.SslMode;
			if (SslMode != MySqlSslMode.None)
			{
				try
				{
					Certificate = new X509Certificate2(csb.CertificateFile, csb.CertificatePassword);
				}
				catch (CryptographicException ex)
				{
					if (!File.Exists(csb.CertificateFile))
						throw new MySqlException("Cannot find SSL Certificate File", ex);
					throw new MySqlException("Either the SSL Certificate Password is incorrect or the SSL Certificate File is invalid", ex);
				}
			}

			// Connection Pooling Options
			Pooling = csb.Pooling;
			ConnectionReset = csb.ConnectionReset;
			if (csb.MinimumPoolSize > csb.MaximumPoolSize)
				throw new MySqlException("MaximumPoolSize must be greater than or equal to MinimumPoolSize");
			MinimumPoolSize = (int)csb.MinimumPoolSize;
			MaximumPoolSize = (int)csb.MaximumPoolSize;

			// Other Options
			AllowUserVariables = csb.AllowUserVariables;
			ConnectionTimeout = (int)csb.ConnectionTimeout;
			ConvertZeroDateTime = csb.ConvertZeroDateTime;
			ForceSynchronous = csb.ForceSynchronous;
			OldGuids = csb.OldGuids;
			PersistSecurityInfo = csb.PersistSecurityInfo;
			UseAffectedRows = csb.UseAffectedRows;
			UseCompression = csb.UseCompression;
		}

		public ConnectionSettings WithUseCompression(bool useCompression) => new ConnectionSettings(this, useCompression);

		private ConnectionSettings(ConnectionSettings other, bool? useCompression)
		{
			ConnectionString = other.ConnectionString;
			Hostnames = other.Hostnames;
			Port = other.Port;
			UserID = other.UserID;
			Password = other.Password;
			Database = other.Database;
			SslMode = other.SslMode;
			Certificate = other.Certificate;
			Pooling = other.Pooling;
			ConnectionReset = other.ConnectionReset;
			MinimumPoolSize = other.MinimumPoolSize;
			MaximumPoolSize = other.MaximumPoolSize;
			AllowUserVariables = other.AllowUserVariables;
			ConnectionTimeout = other.ConnectionTimeout;
			ConvertZeroDateTime = other.ConvertZeroDateTime;
			ForceSynchronous = other.ForceSynchronous;
			OldGuids = other.OldGuids;
			PersistSecurityInfo = other.PersistSecurityInfo;
			UseAffectedRows = other.UseAffectedRows;
			UseCompression = useCompression ?? other.UseCompression;
		}

		internal readonly string ConnectionString;

		// Base Options
		internal readonly IEnumerable<string> Hostnames;
		internal readonly int Port;
		internal readonly string UserID;
		internal readonly string Password;
		internal readonly string Database;

		// SSL/TLS Options
		internal readonly MySqlSslMode SslMode;
		internal readonly X509Certificate2 Certificate;

		// Connection Pooling Options
		internal readonly bool Pooling;
		internal readonly bool ConnectionReset;
		internal readonly int MinimumPoolSize;
		internal readonly int MaximumPoolSize;

		// Other Options
		internal readonly bool AllowUserVariables;
		internal readonly int ConnectionTimeout;
		internal readonly bool ConvertZeroDateTime;
		internal readonly bool ForceSynchronous;
		internal readonly bool OldGuids;
		internal readonly bool PersistSecurityInfo;
		internal readonly bool UseAffectedRows;
		internal readonly bool UseCompression;
	}

}
