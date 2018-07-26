using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
	public enum MySqlCertificateStoreLocation
	{
		/// <summary>
		/// Do not use certificate store
		/// </summary>
		None,
		/// <summary>
		/// Use certificate store for the current user
		/// </summary>
		CurrentUser,
		/// <summary>
		/// User certificate store for the machine
		/// </summary>
		LocalMachine
	}
}
