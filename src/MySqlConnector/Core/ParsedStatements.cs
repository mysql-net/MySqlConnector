using MySqlConnector.Protocol;

namespace MySqlConnector.Core;

/// <summary>
/// <see cref="ParsedStatements"/> wraps a collection of <see cref="ParsedStatement"/> objects.
/// It implements <see cref="IDisposable"/> to return the memory backing the statements to a shared pool.
/// </summary>
internal sealed class ParsedStatements : IDisposable
{
	public IReadOnlyList<ParsedStatement> Statements => m_statements;

	public void Dispose()
	{
		m_statements.Clear();
		m_payloadData.Dispose();
		m_payloadData = default;
	}

	internal ParsedStatements(List<ParsedStatement> statements, PayloadData payloadData)
	{
		m_statements = statements;
		m_payloadData = payloadData;
	}

	private readonly List<ParsedStatement> m_statements;
	private PayloadData m_payloadData;
}
