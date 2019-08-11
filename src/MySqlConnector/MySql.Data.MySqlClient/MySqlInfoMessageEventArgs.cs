using System;
using System.Collections.Generic;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlInfoMessageEventArgs : EventArgs
	{
		internal MySqlInfoMessageEventArgs(MySqlError[] errors) => this.errors = errors;

		public MySqlError[] errors { get; }

		public IReadOnlyList<MySqlError> Errors => errors;
	}

	public delegate void MySqlInfoMessageEventHandler(object sender, MySqlInfoMessageEventArgs args);
}
