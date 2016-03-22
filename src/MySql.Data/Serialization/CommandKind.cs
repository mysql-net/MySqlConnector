namespace MySql.Data.Serialization
{
    internal enum CommandKind
    {
		Quit = 1,
		InitDatabase = 2,
		Query = 3,
		PrepareStatement = 22,
		ExecuteStatement = 23,
		CloseStatement = 25,
    }
}
