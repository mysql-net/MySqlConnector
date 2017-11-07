using System;

namespace MySqlConnector.Protocol.Payloads
{
    internal sealed class EmptyPayload
    {
        public static PayloadData Create()
        {
            return new PayloadData(new ArraySegment<byte>(new byte[] { }));
        }
    }
}
