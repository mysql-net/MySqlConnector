using System;

namespace MySqlConnector.Utilities
{
	/// <summary>
	/// An <see cref="ArraySegment{T}"/> that supports having its underlying array reallocated and resized.
	/// </summary>
	internal readonly struct ResizableArraySegment<T>
	{
		public ResizableArraySegment(ResizableArray<T> array, int offset, int count)
		{
			Array = array;
			Offset = offset;
			Count = count;
		}

		public ResizableArray<T> Array { get; }
		public int Offset { get; }
		public int Count { get; }

		public static implicit operator ReadOnlySpan<T>(ResizableArraySegment<T> segment) => new ReadOnlySpan<T>(segment.Array.Array, segment.Offset, segment.Count);
	}
}
