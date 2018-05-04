using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;

namespace MySqlConnector.Core
{
	/// <summary>
	/// Extension methods on the DiagnosticListener class to log MySqlCommand data
	/// </summary>
	internal static class DiagnosticListenerExtensions
	{
		// Diagnostics listeners name
		public const string CommandListenerName = "MySqlConnector.Command";
		public const string ConnectionListenerName = "MySqlConnector.Connection";
		public const string TransactionListenerName = "MySqlConnector.Transaction";

		// Command listener events name
		public const string WriteStart = nameof(WriteStart);
		public const string WriteStop = nameof(WriteStop);
		public const string WriteError = nameof(WriteError);

		// Connection listener events name
		public const string WriteOpenStart = nameof(WriteOpenStart);
		public const string WriteOpenStop = nameof(WriteOpenStop);
		public const string WriteOpenError = nameof(WriteOpenError);
		public const string WriteCloseStart = nameof(WriteCloseStart);
		public const string WriteCloseStop = nameof(WriteCloseStop);
		public const string WriteCloseError = nameof(WriteCloseError);

		// Transaction listener events name
		public const string WriteCommitStart = nameof(WriteCommitStart);
		public const string WriteCommitStop = nameof(WriteCommitStop);
		public const string WriteCommitError = nameof(WriteCommitError);
		public const string WriteRollbackStart = nameof(WriteRollbackStart);
		public const string WriteRollbackStop = nameof(WriteRollbackStop);
		public const string WriteRollbackError = nameof(WriteRollbackError);

		public static Guid WriteCommandStart(this DiagnosticListener @this, MySqlCommand sqlCommand, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteStart))
			{
				Guid operationId = Guid.NewGuid();

				@this.Write(
					WriteStart,
					new
					{
						OperationId = operationId,
						Operation = operation,
						Command = sqlCommand
					});

				return operationId;
			}
			else
				return Guid.Empty;
		}

		public static void WriteCommandStop(this DiagnosticListener @this, Guid operationId, MySqlCommand sqlCommand, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteStop))
			{
				@this.Write(
					WriteStop,
					new
					{
						OperationId = operationId,
						Operation = operation,
						Command = sqlCommand,
						Timestamp = Stopwatch.GetTimestamp()
					});
			}
		}

		public static void WriteCommandError(this DiagnosticListener @this, Guid operationId, MySqlCommand sqlCommand, Exception ex, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteError))
			{
				@this.Write(
					WriteError,
					new
					{
						OperationId = operationId,
						Operation = operation,
						Command = sqlCommand,
						Exception = ex,
						Timestamp = Stopwatch.GetTimestamp()
					});
			}
		}

		public static Guid WriteConnectionOpenStart(this DiagnosticListener @this, MySqlConnection sqlConnection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteOpenStart))
			{
				Guid operationId = Guid.NewGuid();

				@this.Write(
					WriteOpenStart,
					new
					{
						OperationId = operationId,
						Operation = operation,
						Connection = sqlConnection,
						Timestamp = Stopwatch.GetTimestamp()
					});

				return operationId;
			}
			else
				return Guid.Empty;
		}

		public static void WriteConnectionOpenStop(this DiagnosticListener @this, Guid operationId, MySqlConnection sqlConnection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteOpenStop))
			{
				@this.Write(
					WriteOpenStop,
					new
					{
						OperationId = operationId,
						Operation = operation,
						Connection = sqlConnection,
						Timestamp = Stopwatch.GetTimestamp()
					});
			}
		}

		public static void WriteConnectionOpenError(this DiagnosticListener @this, Guid operationId, MySqlConnection sqlConnection, Exception ex, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteOpenError))
			{
				@this.Write(
					WriteOpenError,
					new
					{
						OperationId = operationId,
						Operation = operation,
						Connection = sqlConnection,
						Exception = ex,
						Timestamp = Stopwatch.GetTimestamp()
					});
			}
		}

		public static Guid WriteConnectionCloseStart(this DiagnosticListener @this, MySqlConnection sqlConnection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteCloseStart))
			{
				Guid operationId = Guid.NewGuid();

				@this.Write(
					WriteCloseStart,
					new
					{
						OperationId = operationId,
						Operation = operation,
						Connection = sqlConnection,
						Timestamp = Stopwatch.GetTimestamp()
					});

				return operationId;
			}
			else
				return Guid.Empty;
		}

		public static void WriteConnectionCloseStop(this DiagnosticListener @this, Guid operationId, string clientConnectionId, MySqlConnection sqlConnection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteCloseStop))
			{
				@this.Write(
					WriteCloseStop,
					new
					{
						OperationId = operationId,
						Operation = operation,
						ConnectionId = clientConnectionId,
						Connection = sqlConnection,
						Timestamp = Stopwatch.GetTimestamp()
					});
			}
		}

		public static void WriteConnectionCloseError(this DiagnosticListener @this, Guid operationId, string clientConnectionId, MySqlConnection sqlConnection, Exception ex, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteCloseError))
			{
				@this.Write(
					WriteCloseError,
					new
					{
						OperationId = operationId,
						Operation = operation,
						ConnectionId = clientConnectionId,
						Connection = sqlConnection,
						Exception = ex,
						Timestamp = Stopwatch.GetTimestamp()
					});
			}
		}

		public static Guid WriteTransactionCommitStart(this DiagnosticListener @this, IsolationLevel isolationLevel, MySqlConnection connection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteCommitStart))
			{
				Guid operationId = Guid.NewGuid();

				@this.Write(
					WriteCommitStart,
					new
					{
						OperationId = operationId,
						Operation = operation,
						IsolationLevel = isolationLevel,
						Connection = connection,
						Timestamp = Stopwatch.GetTimestamp()
					});

				return operationId;
			}
			else
				return Guid.Empty;
		}

		public static void WriteTransactionCommitStop(this DiagnosticListener @this, Guid operationId, IsolationLevel isolationLevel, MySqlConnection connection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteCommitStop))
			{
				@this.Write(
					WriteCommitStop,
					new
					{
						OperationId = operationId,
						Operation = operation,
						IsolationLevel = isolationLevel,
						Connection = connection,
						Timestamp = Stopwatch.GetTimestamp()
					});
			}
		}

		public static void WriteTransactionCommitError(this DiagnosticListener @this, Guid operationId, IsolationLevel isolationLevel, MySqlConnection connection, Exception ex, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteCommitError))
			{
				@this.Write(
					WriteCommitError,
					new
					{
						OperationId = operationId,
						Operation = operation,
						IsolationLevel = isolationLevel,
						Connection = connection,
						Exception = ex,
						Timestamp = Stopwatch.GetTimestamp()
					});
			}
		}

		public static Guid WriteTransactionRollbackStart(this DiagnosticListener @this, IsolationLevel isolationLevel, MySqlConnection connection, string transactionName, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteRollbackStart))
			{
				Guid operationId = Guid.NewGuid();

				@this.Write(
					WriteRollbackStart,
					new
					{
						OperationId = operationId,
						Operation = operation,
						IsolationLevel = isolationLevel,
						Connection = connection,
						TransactionName = transactionName,
						Timestamp = Stopwatch.GetTimestamp()
					});

				return operationId;
			}
			else
				return Guid.Empty;
		}

		public static void WriteTransactionRollbackStop(this DiagnosticListener @this, Guid operationId, IsolationLevel isolationLevel, MySqlConnection connection, string transactionName, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteRollbackStop))
			{
				@this.Write(
					WriteRollbackStop,
					new
					{
						OperationId = operationId,
						Operation = operation,
						IsolationLevel = isolationLevel,
						Connection = connection,
						TransactionName = transactionName,
						Timestamp = Stopwatch.GetTimestamp()
					});
			}
		}

		public static void WriteTransactionRollbackError(this DiagnosticListener @this, Guid operationId, IsolationLevel isolationLevel, MySqlConnection connection, string transactionName, Exception ex, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(WriteRollbackError))
			{
				@this.Write(
					WriteRollbackError,
					new
					{
						OperationId = operationId,
						Operation = operation,
						IsolationLevel = isolationLevel,
						Connection = connection,
						TransactionName = transactionName,
						Exception = ex,
						Timestamp = Stopwatch.GetTimestamp()
					});
			}
		}
	}
}
