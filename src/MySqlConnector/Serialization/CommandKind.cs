namespace MySql.Data.Serialization
{
	internal enum CommandKind
	{
		Quit = 1,
		InitDatabase = 2,
		Query = 3,
		Ping = 14,
		ChangeUser = 17,
		ResetConnection = 31,
	}
}
