using System;

namespace MySql.Data.Serialization
{
	internal class StatementParameter
	{
		public ColumnType Type { get; set; }
		public bool IsUnsigned { get; set; }
		public bool IsNull { get; set; }
		public ArraySegment<byte> Data { get; set; } 
	}
}
