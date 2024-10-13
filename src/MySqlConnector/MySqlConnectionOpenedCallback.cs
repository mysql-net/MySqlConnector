namespace MySqlConnector;

/// <summary>
/// A callback that is invoked when a new <see cref="MySqlConnection"/> is opened.
/// </summary>
/// <param name="context">A <see cref="MySqlConnectionOpenedContext"/> giving information about the connection being opened.</param>
/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.</param>
/// <returns>A <see cref="ValueTask"/> representing the result of the possibly-asynchronous operation.</returns>
public delegate ValueTask MySqlConnectionOpenedCallback(MySqlConnectionOpenedContext context, CancellationToken cancellationToken);
