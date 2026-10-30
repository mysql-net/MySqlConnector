namespace MySqlConnector.Utilities;

/// <summary>
/// A timer (backed by <see cref="TimerQueue"/>) that can be restarted cheaply.
/// </summary>
internal struct ResettableTimer
{
	/// <summary>
	/// Starts (or restarts) the timer so that <paramref name="callback"/> will be invoked in approximately <paramref name="milliseconds"/> milliseconds.
	/// </summary>
	/// <param name="milliseconds">The timeout, in milliseconds.</param>
	/// <param name="callback">The callback that <see cref="TimerQueue"/> will invoke; it must pass the timer ID it receives to <see cref="OnTimerFired"/>.</param>
	public void Start(int milliseconds, Action<int> callback)
	{
		m_deadline = unchecked(Environment.TickCount + milliseconds);

		// if a timer with this timeout is already queued, it will fire no later than the new deadline and re-queue itself
		if (milliseconds == m_milliseconds && m_timerId != 0)
			return;

		m_milliseconds = milliseconds;
		var oldTimerId = Interlocked.Exchange(ref m_timerId, TimerQueue.Instance.Add(milliseconds, callback));
		if (oldTimerId != 0)
			TimerQueue.Instance.Remove(oldTimerId);
	}

	/// <summary>
	/// Stops the timer.
	/// </summary>
	public void Stop()
	{
		var timerId = Interlocked.Exchange(ref m_timerId, 0);
		if (timerId != 0)
			TimerQueue.Instance.Remove(timerId);
	}

	/// <summary>
	/// Handles a timer firing; this must be called by the callback passed to <see cref="Start"/>.
	/// </summary>
	/// <param name="timerId">The timer ID that was passed to the callback.</param>
	/// <param name="callback">The callback that was passed to <see cref="Start"/>.</param>
	/// <returns><c>true</c> if the timeout has elapsed; <c>false</c> if the timer was re-queued (because the deadline moved) or was stopped or restarted.</returns>
	public bool OnTimerFired(int timerId, Action<int> callback)
	{
		// Start may be moving the deadline concurrently; reading the previous one is harmless because it has only passed if the timeout really elapsed
		var remaining = unchecked(m_deadline - Environment.TickCount);
		if (remaining <= 0)
			return Interlocked.CompareExchange(ref m_timerId, 0, timerId) == timerId;

		// the deadline moved, so re-queue the timer, unless this timer was concurrently stopped or replaced
		var newTimerId = TimerQueue.Instance.Add(remaining, callback);
		if (Interlocked.CompareExchange(ref m_timerId, newTimerId, timerId) != timerId)
			TimerQueue.Instance.Remove(newTimerId);
		return false;
	}

	private int m_timerId;
	private int m_deadline;
	private int m_milliseconds;
}
