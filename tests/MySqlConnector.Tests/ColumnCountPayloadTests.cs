using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.Tests;

public class ColumnCountPayloadTests
{
	[Theory]
	[InlineData(new byte[] { 1 }, false, 1, true)]
	[InlineData(new byte[] { 1, 1 }, true, 1, true)]
	[InlineData(new byte[] { 1, 0 }, true, 1, false)]
	[InlineData(new byte[] { 2 }, false, 2, true)]
	[InlineData(new byte[] { 2, 1 }, true, 2, true)]
	[InlineData(new byte[] { 2, 0 }, true, 2, false)]
	[InlineData(new byte[] { 2 }, true, 2, true)]
	public void ParseResultSetHeader(byte[] span, bool supportsOptionalMetadata, int expectedColumnCount, bool expectedMetadataFollows)
	{
		var columnCountPayload = ColumnCountPayload.Create(span.AsSpan(), supportsOptionalMetadata);
		Assert.Equal(expectedColumnCount, columnCountPayload.ColumnCount);
		Assert.Equal(expectedMetadataFollows, columnCountPayload.MetadataFollows);
	}
}
