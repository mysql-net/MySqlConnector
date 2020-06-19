using System;
using System.Collections.Generic;

namespace MySqlConnector
{
	public sealed class MySqlInfoMessageEventArgs : EventArgs
	{
		internal MySqlInfoMessageEventArgs(IReadOnlyList<MySqlError> errors) => Errors = errors;

		public IReadOnlyList<MySqlError> Errors { get; }
	}

	public delegate void MySqlInfoMessageEventHandler(object sender, MySqlInfoMessageEventArgs args);
}
