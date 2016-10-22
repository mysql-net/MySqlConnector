using System;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	internal interface IByteHandler
	{
		/// <summary>
		/// Reads data from this byte handler.
		/// </summary>
		/// <param name="count">The number of bytes to read.</param>
		/// <param name="ioBehavior">The <see cref="IOBehavior"/> to use when reading data.</param>
		/// <returns>An <see cref="ArraySegment{Byte}"/> containing the data that was read. This will contain at most <paramref name="count"/> bytes.
		/// If not all the data was available, fewer than <paramref name="count"/> bytes may be returned. If reading failed, zero bytes will be returned. This
		/// <see cref="ArraySegment{Byte}"/> will be valid to read from until the next time <see cref="ReadBytesAsync"/> or
		/// <see cref="WriteBytesAsync"/> is called.</returns>
		ValueTask<ArraySegment<byte>> ReadBytesAsync(int count, IOBehavior ioBehavior);

		/// <summary>
		/// Writes data to this byte handler.
		/// </summary>
		/// <param name="data">The data to write.</param>
		/// <param name="ioBehavior">The <see cref="IOBehavior"/> to use when writing.</param>
		/// <returns>A <see cref="ValueTask{Int32}"/>. The value of this object is not defined.</returns>
		ValueTask<int> WriteBytesAsync(ArraySegment<byte> data, IOBehavior ioBehavior);
	}
}
