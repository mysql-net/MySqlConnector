namespace MySqlConnector.ColumnReaders;

using System;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using MySqlConnector.Core;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

internal sealed class NullColumnReader : IColumnReader
{
	internal static NullColumnReader Instance { get; } = new NullColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return DBNull.Value;
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		throw new InvalidCastException($"Can't convert {columnDefinition.ColumnType} to Int32");
	}
}
