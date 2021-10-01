namespace MySqlConnector;

/// <summary>
/// <see cref="MySqlInfoMessageEventArgs"/> contains the data supplied to the <see cref="MySqlInfoMessageEventHandler"/> event handler.
/// </summary>
public sealed class MySqlInfoMessageEventArgs : EventArgs
{
	internal MySqlInfoMessageEventArgs(IReadOnlyList<MySqlError> errors) => Errors = errors;

	/// <summary>
	/// The list of errors being reported.
	/// </summary>
	public IReadOnlyList<MySqlError> Errors { get; }
}

/// <summary>
/// Defines the event handler for <see cref="MySqlConnection.InfoMessage"/>.
/// </summary>
/// <param name="sender">The sender. This is the associated <see cref="MySqlConnection"/>.</param>
/// <param name="args">The <see cref="MySqlInfoMessageEventArgs"/> containing the errors.</param>
public delegate void MySqlInfoMessageEventHandler(object sender, MySqlInfoMessageEventArgs args);
