namespace MySql.Data.MySqlClient.Results
{
	internal enum ResultSetState
	{
		None,
		ReadResultSetHeader,
		ReadingRows,
		HasMoreData,
		NoMoreData,
	}
}