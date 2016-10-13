using System;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	/// <summary>
	/// Provides a standard implementation of <see cref="IProtocolLayer"/>.
	/// </summary>
	internal abstract class BaseProtocolLayer : IProtocolLayer
	{
		public void StartNewConversation()
		{
			OnStartNewConveration();
		}

		public abstract ValueTask<ArraySegment<byte>> ReadAsync(int? count, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior);

		public abstract ValueTask<int> WriteAsync(ArraySegment<byte> data, IOBehavior ioBehavior);

		public abstract ValueTask<int> FlushAsync(IOBehavior ioBehavior);

		public IProtocolLayer NextLayer { get; private set; }

		public void Inject(IProtocolLayer injectedLayer)
		{
			if (injectedLayer == null)
				throw new ArgumentNullException(nameof(injectedLayer));

			if (NextLayer is SocketProtocolLayer && injectedLayer is StreamProtocolLayer)
			{
				// upgrade from sockets to SSL
				NextLayer = injectedLayer;
				OnNextLayerChanged();
			}
			else if (NextLayer != null)
			{
				NextLayer.Inject(injectedLayer);
			}
			else
			{
				throw new InvalidOperationException("Can't inject layer of type {0} into this protocol stack.".FormatInvariant(injectedLayer.GetType().Name));
			}
		}

		protected BaseProtocolLayer()
		{
		}

		protected BaseProtocolLayer(IProtocolLayer nextLayer)
		{
			if (nextLayer == null)
				throw new ArgumentNullException(nameof(nextLayer));

			NextLayer = nextLayer;
		}

		protected virtual void OnStartNewConveration()
		{
			NextLayer?.StartNewConversation();
		}

		protected virtual void OnNextLayerChanged()
		{
		}
	}
}
