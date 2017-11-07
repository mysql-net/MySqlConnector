using System;

namespace MySqlConnector.Protocol.Payloads
{
    internal class EmptyPayload
    {
        public static PayloadData Create()
        {
            return new PayloadData(new ArraySegment<byte>(new byte[] { }));
        }
    }
}
