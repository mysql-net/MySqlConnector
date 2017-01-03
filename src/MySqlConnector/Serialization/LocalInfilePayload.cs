using System;
using System.Text;

namespace MySql.Data.Serialization
{
    // See https://dev.mysql.com/doc/internals/en/com-query-response.html#local-infile-request
    internal class LocalInfilePayload
    {
        public const byte Signature = 0xFB;
        public const string InfileStreamPrefix = "@@INFILESTREAM";
        public string FileName { get; }
        public static LocalInfilePayload Create(PayloadData payload)
        {
            return new LocalInfilePayload(payload);
        }
        private LocalInfilePayload(PayloadData payload)
        {
            this.FileName = Encoding.UTF8.GetString(new ArraySegment<byte>(payload.ArraySegment.Array, payload.ArraySegment.Offset + 1, payload.ArraySegment.Count - 1));
        }
    }
}
