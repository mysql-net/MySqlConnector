using System;

namespace MySql.Data.Serialization
{
    internal class EmptyPayload
    {
        public static PayloadData Create()
        {
            return new PayloadData(new ArraySegment<byte>(new byte[] { }));
        }
    }
}
