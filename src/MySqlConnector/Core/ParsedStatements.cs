using MySqlConnector.Protocol;

namespace MySqlConnector.Core;

/// <summary>
/// <see cref="ParsedStatements"/> wraps a collection of <see cref="ParsedStatement"/> objects.
/// It implements <see cref="IDisposable"/> to return the memory backing the statements to a shared pool.
/// </summary>
internal sealed class ParsedStatements(List<ParsedStatement> statements, PayloadData payloadData) : IDisposable
{
	public IReadOnlyList<ParsedStatement> Statements => statements;

	public void Dispose()
	{
		statements.Clear();
		payloadData.Dispose();
		payloadData = default;
	}
}
