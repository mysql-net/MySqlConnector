using System.Text;

namespace MySql.Data.Serialization
{
    internal static class PayloadUtilities
    {
	    public static byte[] CreateEofStringPayload(CommandKind command, string value)
	    {
		    var length = Encoding.UTF8.GetByteCount(value);
		    var payload = new byte[length + 1];
		    payload[0] = (byte) command;
		    Encoding.UTF8.GetBytes(value, 0, value.Length, payload, 1);
		    return payload;
	    }
    }
}
