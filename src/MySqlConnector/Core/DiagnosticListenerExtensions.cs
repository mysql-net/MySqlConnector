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
		public const string DiagnosticListenerName = "MySqlClientDiagnosticListener";

		private const string MySqlClientPrefix = "System.Data.MySqlClient.";

		public const string SqlBeforeExecuteCommand = MySqlClientPrefix + nameof(WriteCommandBefore);
		public const string SqlAfterExecuteCommand = MySqlClientPrefix + nameof(WriteCommandAfter);
		public const string SqlErrorExecuteCommand = MySqlClientPrefix + nameof(WriteCommandError);

		public const string SqlBeforeOpenConnection = MySqlClientPrefix + nameof(WriteConnectionOpenBefore);
		public const string SqlAfterOpenConnection = MySqlClientPrefix + nameof(WriteConnectionOpenAfter);
		public const string SqlErrorOpenConnection = MySqlClientPrefix + nameof(WriteConnectionOpenError);

		public const string SqlBeforeCloseConnection = MySqlClientPrefix + nameof(WriteConnectionCloseBefore);
		public const string SqlAfterCloseConnection = MySqlClientPrefix + nameof(WriteConnectionCloseAfter);
		public const string SqlErrorCloseConnection = MySqlClientPrefix + nameof(WriteConnectionCloseError);

		public const string SqlBeforeCommitTransaction = MySqlClientPrefix + nameof(WriteTransactionCommitBefore);
		public const string SqlAfterCommitTransaction = MySqlClientPrefix + nameof(WriteTransactionCommitAfter);
		public const string SqlErrorCommitTransaction = MySqlClientPrefix + nameof(WriteTransactionCommitError);

		public const string SqlBeforeRollbackTransaction = MySqlClientPrefix + nameof(WriteTransactionRollbackBefore);
		public const string SqlAfterRollbackTransaction = MySqlClientPrefix + nameof(WriteTransactionRollbackAfter);
		public const string SqlErrorRollbackTransaction = MySqlClientPrefix + nameof(WriteTransactionRollbackError);

		public static Guid WriteCommandBefore(this DiagnosticListener @this, MySqlCommand sqlCommand, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(SqlBeforeExecuteCommand))
			{
				Guid operationId = Guid.NewGuid();

				@this.Write(
					SqlBeforeExecuteCommand,
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

		public static void WriteCommandAfter(this DiagnosticListener @this, Guid operationId, MySqlCommand sqlCommand, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(SqlAfterExecuteCommand))
			{
				@this.Write(
					SqlAfterExecuteCommand,
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
			if (@this.IsEnabled(SqlErrorExecuteCommand))
			{
				@this.Write(
					SqlErrorExecuteCommand,
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

		public static Guid WriteConnectionOpenBefore(this DiagnosticListener @this, MySqlConnection sqlConnection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(SqlBeforeOpenConnection))
			{
				Guid operationId = Guid.NewGuid();

				@this.Write(
					SqlBeforeOpenConnection,
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

		public static void WriteConnectionOpenAfter(this DiagnosticListener @this, Guid operationId, MySqlConnection sqlConnection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(SqlAfterOpenConnection))
			{
				@this.Write(
					SqlAfterOpenConnection,
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
			if (@this.IsEnabled(SqlErrorOpenConnection))
			{
				@this.Write(
					SqlErrorOpenConnection,
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

		public static Guid WriteConnectionCloseBefore(this DiagnosticListener @this, MySqlConnection sqlConnection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(SqlBeforeCloseConnection))
			{
				Guid operationId = Guid.NewGuid();

				@this.Write(
					SqlBeforeCloseConnection,
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

		public static void WriteConnectionCloseAfter(this DiagnosticListener @this, Guid operationId, string clientConnectionId, MySqlConnection sqlConnection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(SqlAfterCloseConnection))
			{
				@this.Write(
					SqlAfterCloseConnection,
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
			if (@this.IsEnabled(SqlErrorCloseConnection))
			{
				@this.Write(
					SqlErrorCloseConnection,
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

		public static Guid WriteTransactionCommitBefore(this DiagnosticListener @this, IsolationLevel isolationLevel, MySqlConnection connection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(SqlBeforeCommitTransaction))
			{
				Guid operationId = Guid.NewGuid();

				@this.Write(
					SqlBeforeCommitTransaction,
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

		public static void WriteTransactionCommitAfter(this DiagnosticListener @this, Guid operationId, IsolationLevel isolationLevel, MySqlConnection connection, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(SqlAfterCommitTransaction))
			{
				@this.Write(
					SqlAfterCommitTransaction,
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
			if (@this.IsEnabled(SqlErrorCommitTransaction))
			{
				@this.Write(
					SqlErrorCommitTransaction,
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

		public static Guid WriteTransactionRollbackBefore(this DiagnosticListener @this, IsolationLevel isolationLevel, MySqlConnection connection, string transactionName, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(SqlBeforeRollbackTransaction))
			{
				Guid operationId = Guid.NewGuid();

				@this.Write(
					SqlBeforeRollbackTransaction,
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

		public static void WriteTransactionRollbackAfter(this DiagnosticListener @this, Guid operationId, IsolationLevel isolationLevel, MySqlConnection connection, string transactionName, [CallerMemberName] string operation = "")
		{
			if (@this.IsEnabled(SqlAfterRollbackTransaction))
			{
				@this.Write(
					SqlAfterRollbackTransaction,
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
			if (@this.IsEnabled(SqlErrorRollbackTransaction))
			{
				@this.Write(
					SqlErrorRollbackTransaction,
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
