using System;

namespace MySqlConnector
{
	public sealed class MySqlRowsCopiedEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets a value that indicates whether the bulk copy operation should be aborted.
		/// </summary>
		public bool Abort { get; set; }

		/// <summary>
		/// Gets a value that returns the number of rows copied during the current bulk copy operation.
		/// </summary>
		public long RowsCopied { get; internal set; }

		internal MySqlRowsCopiedEventArgs()
		{
		}
	}

	/// <summary>
	/// Represents the method that handles the <see cref="MySqlBulkCopy.MySqlRowsCopied"/> event of a <see cref="MySqlBulkCopy"/>.
	/// </summary>
	public delegate void MySqlRowsCopiedEventHandler(object sender, MySqlRowsCopiedEventArgs e);
}
