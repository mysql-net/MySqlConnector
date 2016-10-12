namespace MySql.Data.Protocol.Serialization
{
	internal class Conversation : IConversation
	{
		public int GetNextSequenceNumber() => m_sequenceNumber++;

		public void StartNew() => m_sequenceNumber = 0;

		private int m_sequenceNumber;
	}
}
