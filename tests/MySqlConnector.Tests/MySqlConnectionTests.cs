using System.Text;

namespace MySqlConnector.Tests;

public class MySqlConnectionTests
{
	[Theory]
	[InlineData(IsolationLevel.ReadUncommitted, null, false, "\x3A\0\0\0\x03set session transaction isolation level read uncommitted;\x13\0\0\0\x03start transaction;")]
	[InlineData(IsolationLevel.ReadUncommitted, null, true, "\x3C\0\0\0\x03\0\x01set session transaction isolation level read uncommitted;\x15\0\0\0\x03\0\x01start transaction;")]
	[InlineData(IsolationLevel.ReadUncommitted, false, false, "\x3A\0\0\0\x03set session transaction isolation level read uncommitted;\x1E\0\0\0\x03start transaction read write;")]
	[InlineData(IsolationLevel.ReadUncommitted, false, true, "\x3C\0\0\0\x03\0\x01set session transaction isolation level read uncommitted;\x20\0\0\0\x03\0\x01start transaction read write;")]
	[InlineData(IsolationLevel.ReadUncommitted, true, false, "\x3A\0\0\0\x03set session transaction isolation level read uncommitted;\x1D\0\0\0\x03start transaction read only;")]
	[InlineData(IsolationLevel.ReadUncommitted, true, true, "\x3C\0\0\0\x03\0\x01set session transaction isolation level read uncommitted;\x1F\0\0\0\x03\0\x01start transaction read only;")]
	[InlineData(IsolationLevel.ReadCommitted, null, false, "\x38\0\0\0\x03set session transaction isolation level read committed;\x13\0\0\0\x03start transaction;")]
	[InlineData(IsolationLevel.ReadCommitted, null, true, "\x3A\0\0\0\x03\0\x01set session transaction isolation level read committed;\x15\0\0\0\x03\0\x01start transaction;")]
	[InlineData(IsolationLevel.ReadCommitted, false, false, "\x38\0\0\0\x03set session transaction isolation level read committed;\x1E\0\0\0\x03start transaction read write;")]
	[InlineData(IsolationLevel.ReadCommitted, false, true, "\x3A\0\0\0\x03\0\x01set session transaction isolation level read committed;\x20\0\0\0\x03\0\x01start transaction read write;")]
	[InlineData(IsolationLevel.ReadCommitted, true, false, "\x38\0\0\0\x03set session transaction isolation level read committed;\x1D\0\0\0\x03start transaction read only;")]
	[InlineData(IsolationLevel.ReadCommitted, true, true, "\x3A\0\0\0\x03\0\x01set session transaction isolation level read committed;\x1F\0\0\0\x03\0\x01start transaction read only;")]
	[InlineData(IsolationLevel.Serializable, null, false, "\x36\0\0\0\x03set session transaction isolation level serializable;\x13\0\0\0\x03start transaction;")]
	[InlineData(IsolationLevel.Serializable, null, true, "\x38\0\0\0\x03\0\x01set session transaction isolation level serializable;\x15\0\0\0\x03\0\x01start transaction;")]
	[InlineData(IsolationLevel.Serializable, false, false, "\x36\0\0\0\x03set session transaction isolation level serializable;\x1E\0\0\0\x03start transaction read write;")]
	[InlineData(IsolationLevel.Serializable, false, true, "\x38\0\0\0\x03\0\x01set session transaction isolation level serializable;\x20\0\0\0\x03\0\x01start transaction read write;")]
	[InlineData(IsolationLevel.Serializable, true, false, "\x36\0\0\0\x03set session transaction isolation level serializable;\x1D\0\0\0\x03start transaction read only;")]
	[InlineData(IsolationLevel.Serializable, true, true, "\x38\0\0\0\x03\0\x01set session transaction isolation level serializable;\x1F\0\0\0\x03\0\x01start transaction read only;")]
	[InlineData(IsolationLevel.RepeatableRead, null, false, "\x39\0\0\0\x03set session transaction isolation level repeatable read;\x13\0\0\0\x03start transaction;")]
	[InlineData(IsolationLevel.RepeatableRead, null, true, "\x3B\0\0\0\x03\0\x01set session transaction isolation level repeatable read;\x15\0\0\0\x03\0\x01start transaction;")]
	[InlineData(IsolationLevel.RepeatableRead, false, false, "\x39\0\0\0\x03set session transaction isolation level repeatable read;\x1E\0\0\0\x03start transaction read write;")]
	[InlineData(IsolationLevel.RepeatableRead, false, true, "\x3B\0\0\0\x03\0\x01set session transaction isolation level repeatable read;\x20\0\0\0\x03\0\x01start transaction read write;")]
	[InlineData(IsolationLevel.RepeatableRead, true, false, "\x39\0\0\0\x03set session transaction isolation level repeatable read;\x1D\0\0\0\x03start transaction read only;")]
	[InlineData(IsolationLevel.RepeatableRead, true, true, "\x3B\0\0\0\x03\0\x01set session transaction isolation level repeatable read;\x1F\0\0\0\x03\0\x01start transaction read only;")]
	[InlineData(IsolationLevel.Snapshot, null, false, "\x39\0\0\0\x03set session transaction isolation level repeatable read;\x2C\0\0\0\x03start transaction with consistent snapshot;")]
	[InlineData(IsolationLevel.Snapshot, null, true, "\x3B\0\0\0\x03\0\x01set session transaction isolation level repeatable read;\x2E\0\0\0\x03\0\x01start transaction with consistent snapshot;")]
	[InlineData(IsolationLevel.Snapshot, false, false, "\x39\0\0\0\x03set session transaction isolation level repeatable read;\x38\0\0\0\x03start transaction with consistent snapshot, read write;")]
	[InlineData(IsolationLevel.Snapshot, false, true, "\x3B\0\0\0\x03\0\x01set session transaction isolation level repeatable read;\x3A\0\0\0\x03\0\x01start transaction with consistent snapshot, read write;")]
	[InlineData(IsolationLevel.Snapshot, true, false, "\x39\0\0\0\x03set session transaction isolation level repeatable read;\x37\0\0\0\x03start transaction with consistent snapshot, read only;")]
	[InlineData(IsolationLevel.Snapshot, true, true, "\x3B\0\0\0\x03\0\x01set session transaction isolation level repeatable read;\x39\0\0\0\x03\0\x01start transaction with consistent snapshot, read only;")]
	public void GetStartTransactionPayload(IsolationLevel isolationLevel, bool? isReadOnly, bool supportsQueryAttributes, string expected)
	{
		var payload = MySqlConnection.GetStartTransactionPayload(isolationLevel, isReadOnly, supportsQueryAttributes);
		Assert.Equal(expected, Encoding.ASCII.GetString(payload.Span.ToArray()));
	}
}
