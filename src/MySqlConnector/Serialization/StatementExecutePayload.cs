using System;
using System.Collections.Generic;

namespace MySql.Data.Serialization
{
	internal class StatementExecutePayload
	{
		public static PayloadData Create(uint statementId, IList<StatementParameter> parameters)
		{
			var writer = new PayloadWriter();

			writer.WriteByte((byte) CommandKind.ExecuteStatement);
			writer.WriteUInt32(statementId);
			writer.WriteByte(0); // flags == CURSOR_TYPE_NO_CURSOR
			writer.WriteUInt32(1); // iteration count is always 1
			if (parameters.Count > 0)
			{
				int parametersProcessed = 0;
				while (parametersProcessed < parameters.Count)
				{
					byte nullBitmap = 0;
					for (int i = 0; i < Math.Min(8, parameters.Count - parametersProcessed); i++)
					{
						if (parameters[parametersProcessed + i].IsNull)
							nullBitmap |= (byte) (1 << i);
					}
					writer.WriteByte(nullBitmap);
					parametersProcessed += 8;
				}

				writer.WriteByte((byte) (parameters.Count == 0 ? 0 : 1)); // new parameters bound
				foreach (var parameter in parameters)
				{
					writer.WriteByte((byte) parameter.Type);
					writer.WriteByte((byte) (parameter.IsUnsigned ? 0x80 : 0));
				}
				foreach (var parameter in parameters)
					writer.Write(parameter.Data);
			}

			return new PayloadData(new ArraySegment<byte>(writer.ToBytes()));
		}
	}
}
