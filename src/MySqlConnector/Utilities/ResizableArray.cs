namespace MySqlConnector.Utilities;

/// <summary>
/// A wrapper around a resizable array. This type is intended to be used with <see cref="ResizableArraySegment{T}"/>.
/// </summary>
internal sealed class ResizableArray<T>
	where T : notnull
{
	public T[]? Array => m_array;
	public int Count => m_array?.Length ?? 0;

	/// <summary>
	/// Do not call this method directly; use <see cref="Utility.Resize{T}"/>.
	/// </summary>
	internal void DoResize(int length)
	{
		if (m_array is null || length > m_array.Length)
			System.Array.Resize(ref m_array, Math.Max(length, Count * 2));
	}

	private T[]? m_array;
}
