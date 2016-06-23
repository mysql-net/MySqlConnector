namespace MySql.Data.Serialization
{
	internal enum CommandKind
	{
		Quit = 1,
		InitDatabase = 2,
		Query = 3,
		ChangeUser = 17,
		PrepareStatement = 22,
		ExecuteStatement = 23,
		CloseStatement = 25,
		ResetConnection = 31,
	}
}
