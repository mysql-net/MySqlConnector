using System;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	/// <summary>
	/// <see cref="IProtocolLayer"/> represents a discrete stage in the process of serializing data for the MySQL protocol.
	/// </summary>
	/// <remarks>See <a href="https://dev.mysql.com/doc/internals/en/client-server-protocol.html">MySQL Client/Server Protocol</a> for technical details on the protocol itself.</remarks>
	internal interface IProtocolLayer
	{
		/// <summary>
		/// Starts a new "conversation" with the MySQL Server. This resets the "<a href="https://dev.mysql.com/doc/internals/en/sequence-id.html">sequence id</a>"
		/// and should be called when a new command begins.
		/// </summary>
		void StartNewConversation();

		/// <summary>
		/// Reads data from this protocol layer.
		/// </summary>
		/// <param name="count">If known, the number of bytes to read; specific layers may be able to provide a more optimized
		/// implementation if the amount of data to read is known in advance.</param>
		/// <param name="protocolErrorBehavior">The <see cref="ProtocolErrorBehavior"/> to use if there is a protocol error.</param>
		/// <param name="ioBehavior">The <see cref="IOBehavior"/> to use when reading data.</param>
		/// <returns>An <see cref="ArraySegment{Byte}"/> containing the data that was read. If <paramref name="count"/> was <c>null</c>,
		/// this will be a "logical unit". If <paramref name="count"/> was specified, this will contain <paramref name="count"/> bytes
		/// unless reading the data failed (in which case fewer than <paramref name="count"/> bytes may be returned). This
		/// <see cref="ArraySegment{Byte}"/> will be valid to read from until the next time <see cref="ReadAsync"/> or
		/// <see cref="WriteAsync"/> is called.</returns>
		ValueTask<ArraySegment<byte>> ReadAsync(int? count, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior);

		/// <summary>
		/// Writes data to this protocol layer.
		/// </summary>
		/// <param name="data">The data to write.</param>
		/// <param name="ioBehavior">The <see cref="IOBehavior"/> to use when writing.</param>
		/// <returns>A <see cref="ValueTask{Int32}"/>. The value of this object is not defined.</returns>
		ValueTask<int> WriteAsync(ArraySegment<byte> data, IOBehavior ioBehavior);

		/// <summary>
		/// Indicates that data has finished being written and should be flushed to the next protocol layer. The specific behavior of this
		/// method is defined by each <see cref="IProtocolLayer"/> implementation.
		/// </summary>
		/// <param name="ioBehavior">The <see cref="IOBehavior"/> to use when flushing.</param>
		/// <returns>A <see cref="ValueTask{Int32}"/>. The value of this object is not defined.</returns>
		ValueTask<int> FlushAsync(IOBehavior ioBehavior);

		/// <summary>
		/// Returns the next <see cref="IProtocolLayer"/> "below" this one.
		/// </summary>
		IProtocolLayer NextLayer { get; }

		/// <summary>
		/// Injects a new protocol layer into the stack. The specific behavior of this method will depend on the current
		/// protocol layer stack and the type of the layer being injected.
		/// </summary>
		/// <param name="injectedLayer">The <see cref="IProtocolLayer"/> to inject.</param>
		void Inject(IProtocolLayer injectedLayer);
	}
}
