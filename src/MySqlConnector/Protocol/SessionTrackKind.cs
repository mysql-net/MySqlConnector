namespace MySqlConnector.Protocol
{
	internal enum SessionTrackKind : byte
	{
		/// <summary>
		/// SESSION_TRACK_SYSTEM_VARIABLES: one or more system variables changed
		/// </summary>
		SystemVariables = 0,

		/// <summary>
		/// SESSION_TRACK_SCHEMA: schema changed
		/// </summary>
		Schema = 1,

		/// <summary>
		/// SESSION_TRACK_STATE_CHANGE: "track state change" changed
		/// </summary>
		StateChange = 2,

		/// <summary>
		/// SESSION_TRACK_GTIDS: "track GTIDs" changed
		/// </summary>
		Gtids = 3,
	}
}
