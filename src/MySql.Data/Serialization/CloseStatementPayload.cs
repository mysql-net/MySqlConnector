using System;

namespace MySql.Data.Serialization
{
    internal class CloseStatementPayload
    {
	    public static PayloadData Create(uint statementId)
	    {
		    byte[] payload = new byte[5];
		    payload[0] = (byte) CommandKind.CloseStatement;
		    SerializationUtility.WriteUInt32(statementId, payload, 1, 4);
			return new PayloadData(new ArraySegment<byte>(payload));
	    }
    }
}
