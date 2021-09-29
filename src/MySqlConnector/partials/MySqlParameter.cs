using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlConnector
{
	public partial class MySqlParameter
	{
		/// <summary>
		/// 是否自动对齐时区
		/// </summary>
		public static bool AutoChangeTimeZone { get; set; } = true;

	}
}
