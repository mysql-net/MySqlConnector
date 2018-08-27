using System;

namespace MySqlConnector.Protocol.Payloads
{
	internal readonly struct EmptyPayload
    {
        public static PayloadData Create()
        {
#if NET45
			byte[] data = new byte[0];
#else
			byte[] data = Array.Empty<byte>();
#endif
			return new PayloadData(data);
        }
    }
}
