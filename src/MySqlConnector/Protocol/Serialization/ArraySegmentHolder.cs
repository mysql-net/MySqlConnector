using System;

namespace MySqlConnector.Protocol.Serialization
{
	/// <summary>
	/// <see cref="ArraySegmentHolder{T}"/> is a class that holds an instance of <see cref="ArraySegment{T}"/>.
	/// Its primary difference from <see cref="ArraySegment{T}"/> is that it's a reference type, so mutations
	/// to this object are visible to other objects that hold a reference to it.
	/// </summary>
	internal sealed class ArraySegmentHolder<T>
	{
		public ArraySegment<T> ArraySegment { get; set; }

		public T[] Array => ArraySegment.Array;
		public int Offset => ArraySegment.Offset;
		public int Count => ArraySegment.Count;

		public void Clear()
		{
			if (ArraySegment.Count > 0)
				ArraySegment = new ArraySegment<T>(ArraySegment.Array, 0, 0);
		}
	}
}
