using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.Serialization
{
	internal abstract class Packet
	{
		public Task WriteAsync(Stream destination, CancellationToken cancellationToken)
		{
			return WriteAsyncCore(destination, cancellationToken);
		}

		protected virtual Task WriteAsyncCore(Stream destination, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}
	}
}
