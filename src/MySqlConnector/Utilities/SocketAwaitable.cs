#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace MySqlConnector.Utilities;

// See https://devblogs.microsoft.com/pfxteam/awaiting-socket-operations/
internal sealed class SocketAwaitable : INotifyCompletion
{
	public SocketAwaitable(SocketAsyncEventArgs eventArgs)
	{
		EventArgs = eventArgs ?? throw new ArgumentNullException(nameof(eventArgs));
		eventArgs.Completed += (s, e) => (m_continuation ?? Interlocked.CompareExchange(ref m_continuation, s_sentinel, null))?.Invoke();
	}

	public SocketAwaitable GetAwaiter() => this;
	public bool IsCompleted => WasCompleted;

	public void OnCompleted(Action continuation)
	{
		if (m_continuation == s_sentinel || Interlocked.CompareExchange(ref m_continuation, continuation, null) == s_sentinel)
			Task.Run(continuation);
	}

	public void GetResult()
	{
		if (EventArgs.SocketError != SocketError.Success)
			throw new SocketException((int) EventArgs.SocketError);
	}

	internal bool WasCompleted { get; set; }
	internal SocketAsyncEventArgs EventArgs { get; }

	internal void Reset()
	{
		WasCompleted = false;
		m_continuation = null;
	}

	private static readonly Action s_sentinel = () => { };

	private Action? m_continuation;
}
#endif
