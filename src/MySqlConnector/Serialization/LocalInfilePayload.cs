using System;
using System.Text;

namespace MySql.Data.Serialization
{
	// See https://dev.mysql.com/doc/internals/en/com-query-response.html#local-infile-request
	internal class LocalInfilePayload
	{
		public const byte Signature = 0xFB;

		public string FileName { get; }

		public static LocalInfilePayload Create(PayloadData payload) => new LocalInfilePayload(payload);

		private LocalInfilePayload(PayloadData payload)
		{
			FileName = Utility.GetString(Encoding.UTF8, Utility.Slice(payload.ArraySegment, 1));
		}
	}
}
