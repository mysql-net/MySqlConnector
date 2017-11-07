using System;

namespace MySqlConnector.Protocol
{
	[Flags]
	internal enum ServerStatus : ushort
	{
		/// <summary>
		/// A transaction is active.
		/// </summary>
		InTransaction = 1,

		/// <summary>
		/// Auto-commit is enabled
		/// </summary>
		AutoCommit = 2,

		MoreResultsExist = 8,

		NoGoodIndexUsed = 0x10,

		NoIndexUsed = 0x20,

		/// <summary>
		/// Used by Binary Protocol Resultset to signal that COM_STMT_FETCH must be used to fetch the row-data.
		/// </summary>
		CursorExists = 0x40,

		LastRowSent = 0x80,

		DatabaseDropped = 0x100,

		NoBackslashEscapes = 0x200,

		MetadataChanged = 0x400,

		QueryWasSlow = 0x800,

		PsOutParams = 0x1000,

		/// <summary>
		/// In a read-only transaction.
		/// </summary>
		InReadOnlyTransaction = 0x2000,

		/// <summary>
		/// Connection state information has changed.
		/// </summary>
		SessionStateChanged = 0x4000,
	}
}
