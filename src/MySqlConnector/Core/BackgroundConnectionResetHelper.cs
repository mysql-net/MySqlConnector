using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Logging;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core
{
	internal static class BackgroundConnectionResetHelper
	{
		public static void AddSession(ServerSession session, MySqlConnection? owningConnection)
		{
			var resetTask = session.TryResetConnectionAsync(session.Pool!.ConnectionSettings, owningConnection, true, IOBehavior.Asynchronous, default);
			lock (s_lock)
				s_resetTasks.Add(resetTask);

			if (Log.IsTraceEnabled())
				Log.Trace("Started Session{0} reset in background; waiting TaskCount: {1}.", session.Id, s_resetTasks.Count);

			// release only if it is likely to succeed
			if (s_semaphore.CurrentCount == 0)
			{
				Log.Trace("Releasing semaphore.");
				try
				{
					s_semaphore.Release();
				}
				catch (SemaphoreFullException)
				{
					// ignore
				}
			}
		}

		public static void Start()
		{
			Log.Info("Starting BackgroundConnectionResetHelper worker.");
			lock (s_lock)
			{
				if (s_workerTask is null)
					s_workerTask = Task.Run(async () => await ReturnSessionsAsync());
			}
		}

		public static void Stop()
		{
			Log.Info("Stopping BackgroundConnectionResetHelper worker.");
			s_cancellationTokenSource.Cancel();
			Task? workerTask;
			lock (s_lock)
				workerTask = s_workerTask;

			if (workerTask is not null)
			{
				try
				{
					workerTask.GetAwaiter().GetResult();
				}
				catch (OperationCanceledException)
				{
				}
			}
			Log.Info("Stopped BackgroundConnectionResetHelper worker.");
		}

		public static async Task ReturnSessionsAsync()
		{
			Log.Info("Started BackgroundConnectionResetHelper worker.");

			List<Task<bool>> localTasks = new();

			// keep running until stopped
			while (!s_cancellationTokenSource.IsCancellationRequested)
			{
				try
				{
					// block until AddSession releases the semaphore
					Log.Trace("Waiting for semaphore.");
					await s_semaphore.WaitAsync(s_cancellationTokenSource.Token).ConfigureAwait(false);

					// process all sessions that have started being returned
					while (true)
					{
						lock (s_lock)
						{
							localTasks.AddRange(s_resetTasks);
							s_resetTasks.Clear();
						}

						if (localTasks.Count == 0)
							break;

						if (Log.IsTraceEnabled())
							Log.Trace("Found TaskCount {0} task(s) to process.", localTasks.Count);

						await Task.WhenAll(localTasks);
						localTasks.Clear();
					}
				}
				catch (Exception ex) when (!(ex is OperationCanceledException oce && oce.CancellationToken == s_cancellationTokenSource.Token))
				{
					Log.Error("Unhandled exception: {0}", ex);
				}
			}
		}

		static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(BackgroundConnectionResetHelper));
		static readonly object s_lock = new();
		static readonly SemaphoreSlim s_semaphore = new(1, 1);
		static readonly CancellationTokenSource s_cancellationTokenSource = new();
		static readonly List<Task<bool>> s_resetTasks = new();
		static Task? s_workerTask;
	}
}
