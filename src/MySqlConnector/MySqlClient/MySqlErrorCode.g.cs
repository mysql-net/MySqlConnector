namespace MySql.Data.MySqlClient
{
	/// <summary>
	/// MySQL Server error codes. Taken from <a href="https://dev.mysql.com/doc/refman/5.7/en/error-messages-server.html">Server Error Codes and Messages</a>.
	/// </summary>
	[System.CodeDom.Compiler.GeneratedCode("https://gist.github.com/bgrainger/791cecb647d514a9dd2f3d83b2387e49", "2")]
	public enum MySqlErrorCode
	{
		/// <summary>
		/// The timeout period specified by <see cref="MySqlCommand.CommandTimeout"/> elapsed before the operation completed.
		/// </summary>
		CommandTimeoutExpired = -1,

		None = 0,

		/// <summary>
		/// ER_HASHCHK
		/// </summary>
		HashCheck = 1000,

		/// <summary>
		/// ER_NISAMCHK
		/// </summary>
		ISAMCheck = 1001,

		/// <summary>
		/// ER_NO
		/// </summary>
		No = 1002,

		/// <summary>
		/// ER_YES
		/// </summary>
		Yes = 1003,

		/// <summary>
		/// ER_CANT_CREATE_FILE
		/// </summary>
		CannotCreateFile = 1004,

		/// <summary>
		/// ER_CANT_CREATE_TABLE
		/// </summary>
		CannotCreateTable = 1005,

		/// <summary>
		/// ER_CANT_CREATE_DB
		/// </summary>
		CannotCreateDatabase = 1006,

		/// <summary>
		/// ER_DB_CREATE_EXISTS
		/// </summary>
		DatabaseCreateExists = 1007,

		/// <summary>
		/// ER_DB_DROP_EXISTS
		/// </summary>
		DatabaseDropExists = 1008,

		/// <summary>
		/// ER_DB_DROP_DELETE
		/// </summary>
		DatabaseDropDelete = 1009,

		/// <summary>
		/// ER_DB_DROP_RMDIR
		/// </summary>
		DatabaseDropRemoveDir = 1010,

		/// <summary>
		/// ER_CANT_DELETE_FILE
		/// </summary>
		CannotDeleteFile = 1011,

		/// <summary>
		/// ER_CANT_FIND_SYSTEM_REC
		/// </summary>
		CannotFindSystemRecord = 1012,

		/// <summary>
		/// ER_CANT_GET_STAT
		/// </summary>
		CannotGetStatus = 1013,

		/// <summary>
		/// ER_CANT_GET_WD
		/// </summary>
		CannotGetWorkingDirectory = 1014,

		/// <summary>
		/// ER_CANT_LOCK
		/// </summary>
		CannotLock = 1015,

		/// <summary>
		/// ER_CANT_OPEN_FILE
		/// </summary>
		CannotOpenFile = 1016,

		/// <summary>
		/// ER_FILE_NOT_FOUND
		/// </summary>
		FileNotFound = 1017,

		/// <summary>
		/// ER_CANT_READ_DIR
		/// </summary>
		CannotReadDirectory = 1018,

		/// <summary>
		/// ER_CANT_SET_WD
		/// </summary>
		CannotSetWorkingDirectory = 1019,

		/// <summary>
		/// ER_CHECKREAD
		/// </summary>
		CheckRead = 1020,

		/// <summary>
		/// ER_DISK_FULL
		/// </summary>
		DiskFull = 1021,

		/// <summary>
		/// ER_DUP_KEY
		/// </summary>
		DuplicateKey = 1022,

		/// <summary>
		/// ER_ERROR_ON_CLOSE
		/// </summary>
		ErrorOnClose = 1023,

		/// <summary>
		/// ER_ERROR_ON_READ
		/// </summary>
		ErrorOnRead = 1024,

		/// <summary>
		/// ER_ERROR_ON_RENAME
		/// </summary>
		ErrorOnRename = 1025,

		/// <summary>
		/// ER_ERROR_ON_WRITE
		/// </summary>
		ErrorOnWrite = 1026,

		/// <summary>
		/// ER_FILE_USED
		/// </summary>
		FileUsed = 1027,

		/// <summary>
		/// ER_FILSORT_ABORT
		/// </summary>
		FileSortAborted = 1028,

		/// <summary>
		/// ER_FORM_NOT_FOUND
		/// </summary>
		FormNotFound = 1029,

		/// <summary>
		/// ER_GET_ERRNO
		/// </summary>
		GetErrorNumber = 1030,

		/// <summary>
		/// ER_ILLEGAL_HA
		/// </summary>
		IllegalHA = 1031,

		/// <summary>
		/// ER_KEY_NOT_FOUND
		/// </summary>
		KeyNotFound = 1032,

		/// <summary>
		/// ER_NOT_FORM_FILE
		/// </summary>
		NotFormFile = 1033,

		/// <summary>
		/// ER_NOT_KEYFILE
		/// </summary>
		NotKeyFile = 1034,

		/// <summary>
		/// ER_OLD_KEYFILE
		/// </summary>
		OldKeyFile = 1035,

		/// <summary>
		/// ER_OPEN_AS_READONLY
		/// </summary>
		OpenAsReadOnly = 1036,

		/// <summary>
		/// ER_OUTOFMEMORY
		/// </summary>
		OutOfMemory = 1037,

		/// <summary>
		/// ER_OUT_OF_SORTMEMORY
		/// </summary>
		OutOfSortMemory = 1038,

		/// <summary>
		/// ER_UNEXPECTED_EOF
		/// </summary>
		UnexepectedEOF = 1039,

		/// <summary>
		/// ER_CON_COUNT_ERROR
		/// </summary>
		ConnectionCountError = 1040,

		/// <summary>
		/// ER_OUT_OF_RESOURCES
		/// </summary>
		OutOfResources = 1041,

		/// <summary>
		/// ER_BAD_HOST_ERROR
		/// </summary>
		UnableToConnectToHost = 1042,

		/// <summary>
		/// ER_HANDSHAKE_ERROR
		/// </summary>
		HandshakeError = 1043,

		/// <summary>
		/// ER_DBACCESS_DENIED_ERROR
		/// </summary>
		DatabaseAccessDenied = 1044,

		/// <summary>
		/// ER_ACCESS_DENIED_ERROR
		/// </summary>
		AccessDenied = 1045,

		/// <summary>
		/// ER_NO_DB_ERROR
		/// </summary>
		NoDatabaseSelected = 1046,

		/// <summary>
		/// ER_UNKNOWN_COM_ERROR
		/// </summary>
		UnknownCommand = 1047,

		/// <summary>
		/// ER_BAD_NULL_ERROR
		/// </summary>
		ColumnCannotBeNull = 1048,

		/// <summary>
		/// ER_BAD_DB_ERROR
		/// </summary>
		UnknownDatabase = 1049,

		/// <summary>
		/// ER_TABLE_EXISTS_ERROR
		/// </summary>
		TableExists = 1050,

		/// <summary>
		/// ER_BAD_TABLE_ERROR
		/// </summary>
		BadTable = 1051,

		/// <summary>
		/// ER_NON_UNIQ_ERROR
		/// </summary>
		NonUnique = 1052,

		/// <summary>
		/// ER_SERVER_SHUTDOWN
		/// </summary>
		ServerShutdown = 1053,

		/// <summary>
		/// ER_BAD_FIELD_ERROR
		/// </summary>
		BadFieldError = 1054,

		/// <summary>
		/// ER_WRONG_FIELD_WITH_GROUP
		/// </summary>
		WrongFieldWithGroup = 1055,

		/// <summary>
		/// ER_WRONG_GROUP_FIELD
		/// </summary>
		WrongGroupField = 1056,

		/// <summary>
		/// ER_WRONG_SUM_SELECT
		/// </summary>
		WrongSumSelected = 1057,

		/// <summary>
		/// ER_WRONG_VALUE_COUNT
		/// </summary>
		WrongValueCount = 1058,

		/// <summary>
		/// ER_TOO_LONG_IDENT
		/// </summary>
		TooLongIdentifier = 1059,

		/// <summary>
		/// ER_DUP_FIELDNAME
		/// </summary>
		DuplicateFieldName = 1060,

		/// <summary>
		/// ER_DUP_KEYNAME
		/// </summary>
		DuplicateKeyName = 1061,

		/// <summary>
		/// ER_DUP_ENTRY
		/// </summary>
		DuplicateKeyEntry = 1062,

		/// <summary>
		/// ER_WRONG_FIELD_SPEC
		/// </summary>
		WrongFieldSpecifier = 1063,

		/// <summary>
		/// You have an error in your SQL syntax (ER_PARSE_ERROR).
		/// </summary>
		ParseError = 1064,

		/// <summary>
		/// ER_EMPTY_QUERY
		/// </summary>
		EmptyQuery = 1065,

		/// <summary>
		/// ER_NONUNIQ_TABLE
		/// </summary>
		NonUniqueTable = 1066,

		/// <summary>
		/// ER_INVALID_DEFAULT
		/// </summary>
		InvalidDefault = 1067,

		/// <summary>
		/// ER_MULTIPLE_PRI_KEY
		/// </summary>
		MultiplePrimaryKey = 1068,

		/// <summary>
		/// ER_TOO_MANY_KEYS
		/// </summary>
		TooManyKeys = 1069,

		/// <summary>
		/// ER_TOO_MANY_KEY_PARTS
		/// </summary>
		TooManyKeysParts = 1070,

		/// <summary>
		/// ER_TOO_LONG_KEY
		/// </summary>
		TooLongKey = 1071,

		/// <summary>
		/// ER_KEY_COLUMN_DOES_NOT_EXITS
		/// </summary>
		KeyColumnDoesNotExist = 1072,

		/// <summary>
		/// ER_BLOB_USED_AS_KEY
		/// </summary>
		BlobUsedAsKey = 1073,

		/// <summary>
		/// ER_TOO_BIG_FIELDLENGTH
		/// </summary>
		TooBigFieldLength = 1074,

		/// <summary>
		/// ER_WRONG_AUTO_KEY
		/// </summary>
		WrongAutoKey = 1075,

		/// <summary>
		/// ER_READY
		/// </summary>
		Ready = 1076,

		/// <summary>
		/// ER_NORMAL_SHUTDOWN
		/// </summary>
		NormalShutdown = 1077,

		/// <summary>
		/// ER_GOT_SIGNAL
		/// </summary>
		GotSignal = 1078,

		/// <summary>
		/// ER_SHUTDOWN_COMPLETE
		/// </summary>
		ShutdownComplete = 1079,

		/// <summary>
		/// ER_FORCING_CLOSE
		/// </summary>
		ForcingClose = 1080,

		/// <summary>
		/// ER_IPSOCK_ERROR
		/// </summary>
		IPSocketError = 1081,

		/// <summary>
		/// ER_NO_SUCH_INDEX
		/// </summary>
		NoSuchIndex = 1082,

		/// <summary>
		/// ER_WRONG_FIELD_TERMINATORS
		/// </summary>
		WrongFieldTerminators = 1083,

		/// <summary>
		/// ER_BLOBS_AND_NO_TERMINATED
		/// </summary>
		BlobsAndNoTerminated = 1084,

		/// <summary>
		/// ER_TEXTFILE_NOT_READABLE
		/// </summary>
		TextFileNotReadable = 1085,

		/// <summary>
		/// ER_FILE_EXISTS_ERROR
		/// </summary>
		FileExists = 1086,

		/// <summary>
		/// ER_LOAD_INFO
		/// </summary>
		LoadInfo = 1087,

		/// <summary>
		/// ER_ALTER_INFO
		/// </summary>
		AlterInfo = 1088,

		/// <summary>
		/// ER_WRONG_SUB_KEY
		/// </summary>
		WrongSubKey = 1089,

		/// <summary>
		/// ER_CANT_REMOVE_ALL_FIELDS
		/// </summary>
		CannotRemoveAllFields = 1090,

		/// <summary>
		/// ER_CANT_DROP_FIELD_OR_KEY
		/// </summary>
		CannotDropFieldOrKey = 1091,

		/// <summary>
		/// ER_INSERT_INFO
		/// </summary>
		InsertInfo = 1092,

		/// <summary>
		/// ER_UPDATE_TABLE_USED
		/// </summary>
		UpdateTableUsed = 1093,

		/// <summary>
		/// ER_NO_SUCH_THREAD
		/// </summary>
		NoSuchThread = 1094,

		/// <summary>
		/// ER_KILL_DENIED_ERROR
		/// </summary>
		KillDenied = 1095,

		/// <summary>
		/// ER_NO_TABLES_USED
		/// </summary>
		NoTablesUsed = 1096,

		/// <summary>
		/// ER_TOO_BIG_SET
		/// </summary>
		TooBigSet = 1097,

		/// <summary>
		/// ER_NO_UNIQUE_LOGFILE
		/// </summary>
		NoUniqueLogFile = 1098,

		/// <summary>
		/// ER_TABLE_NOT_LOCKED_FOR_WRITE
		/// </summary>
		TableNotLockedForWrite = 1099,

		/// <summary>
		/// ER_TABLE_NOT_LOCKED
		/// </summary>
		TableNotLocked = 1100,

		/// <summary>
		/// ER_BLOB_CANT_HAVE_DEFAULT
		/// </summary>
		BlobCannotHaveDefault = 1101,

		/// <summary>
		/// ER_WRONG_DB_NAME
		/// </summary>
		WrongDatabaseName = 1102,

		/// <summary>
		/// ER_WRONG_TABLE_NAME
		/// </summary>
		WrongTableName = 1103,

		/// <summary>
		/// ER_TOO_BIG_SELECT
		/// </summary>
		TooBigSelect = 1104,

		/// <summary>
		/// ER_UNKNOWN_ERROR
		/// </summary>
		UnknownError = 1105,

		/// <summary>
		/// ER_UNKNOWN_PROCEDURE
		/// </summary>
		UnknownProcedure = 1106,

		/// <summary>
		/// ER_WRONG_PARAMCOUNT_TO_PROCEDURE
		/// </summary>
		WrongParameterCountToProcedure = 1107,

		/// <summary>
		/// ER_WRONG_PARAMETERS_TO_PROCEDURE
		/// </summary>
		WrongParametersToProcedure = 1108,

		/// <summary>
		/// ER_UNKNOWN_TABLE
		/// </summary>
		UnknownTable = 1109,

		/// <summary>
		/// ER_FIELD_SPECIFIED_TWICE
		/// </summary>
		FieldSpecifiedTwice = 1110,

		/// <summary>
		/// ER_INVALID_GROUP_FUNC_USE
		/// </summary>
		InvalidGroupFunctionUse = 1111,

		/// <summary>
		/// ER_UNSUPPORTED_EXTENSION
		/// </summary>
		UnsupportedExtenstion = 1112,

		/// <summary>
		/// ER_TABLE_MUST_HAVE_COLUMNS
		/// </summary>
		TableMustHaveColumns = 1113,

		/// <summary>
		/// ER_RECORD_FILE_FULL
		/// </summary>
		RecordFileFull = 1114,

		/// <summary>
		/// ER_UNKNOWN_CHARACTER_SET
		/// </summary>
		UnknownCharacterSet = 1115,

		/// <summary>
		/// ER_TOO_MANY_TABLES
		/// </summary>
		TooManyTables = 1116,

		/// <summary>
		/// ER_TOO_MANY_FIELDS
		/// </summary>
		TooManyFields = 1117,

		/// <summary>
		/// ER_TOO_BIG_ROWSIZE
		/// </summary>
		TooBigRowSize = 1118,

		/// <summary>
		/// ER_STACK_OVERRUN
		/// </summary>
		StackOverrun = 1119,

		/// <summary>
		/// ER_WRONG_OUTER_JOIN
		/// </summary>
		WrongOuterJoin = 1120,

		/// <summary>
		/// ER_NULL_COLUMN_IN_INDEX
		/// </summary>
		NullColumnInIndex = 1121,

		/// <summary>
		/// ER_CANT_FIND_UDF
		/// </summary>
		CannotFindUDF = 1122,

		/// <summary>
		/// ER_CANT_INITIALIZE_UDF
		/// </summary>
		CannotInitializeUDF = 1123,

		/// <summary>
		/// ER_UDF_NO_PATHS
		/// </summary>
		UDFNoPaths = 1124,

		/// <summary>
		/// ER_UDF_EXISTS
		/// </summary>
		UDFExists = 1125,

		/// <summary>
		/// ER_CANT_OPEN_LIBRARY
		/// </summary>
		CannotOpenLibrary = 1126,

		/// <summary>
		/// ER_CANT_FIND_DL_ENTRY
		/// </summary>
		CannotFindDLEntry = 1127,

		/// <summary>
		/// ER_FUNCTION_NOT_DEFINED
		/// </summary>
		FunctionNotDefined = 1128,

		/// <summary>
		/// ER_HOST_IS_BLOCKED
		/// </summary>
		HostIsBlocked = 1129,

		/// <summary>
		/// ER_HOST_NOT_PRIVILEGED
		/// </summary>
		HostNotPrivileged = 1130,

		/// <summary>
		/// ER_PASSWORD_ANONYMOUS_USER
		/// </summary>
		AnonymousUser = 1131,

		/// <summary>
		/// ER_PASSWORD_NOT_ALLOWED
		/// </summary>
		PasswordNotAllowed = 1132,

		/// <summary>
		/// ER_PASSWORD_NO_MATCH
		/// </summary>
		PasswordNoMatch = 1133,

		/// <summary>
		/// ER_UPDATE_INFO
		/// </summary>
		UpdateInfo = 1134,

		/// <summary>
		/// ER_CANT_CREATE_THREAD
		/// </summary>
		CannotCreateThread = 1135,

		/// <summary>
		/// ER_WRONG_VALUE_COUNT_ON_ROW
		/// </summary>
		WrongValueCountOnRow = 1136,

		/// <summary>
		/// ER_CANT_REOPEN_TABLE
		/// </summary>
		CannotReopenTable = 1137,

		/// <summary>
		/// ER_INVALID_USE_OF_NULL
		/// </summary>
		InvalidUseOfNull = 1138,

		/// <summary>
		/// ER_REGEXP_ERROR
		/// </summary>
		RegExpError = 1139,

		/// <summary>
		/// ER_MIX_OF_GROUP_FUNC_AND_FIELDS
		/// </summary>
		MixOfGroupFunctionAndFields = 1140,

		/// <summary>
		/// ER_NONEXISTING_GRANT
		/// </summary>
		NonExistingGrant = 1141,

		/// <summary>
		/// ER_TABLEACCESS_DENIED_ERROR
		/// </summary>
		TableAccessDenied = 1142,

		/// <summary>
		/// ER_COLUMNACCESS_DENIED_ERROR
		/// </summary>
		ColumnAccessDenied = 1143,

		/// <summary>
		/// ER_ILLEGAL_GRANT_FOR_TABLE
		/// </summary>
		IllegalGrantForTable = 1144,

		/// <summary>
		/// ER_GRANT_WRONG_HOST_OR_USER
		/// </summary>
		GrantWrongHostOrUser = 1145,

		/// <summary>
		/// ER_NO_SUCH_TABLE
		/// </summary>
		NoSuchTable = 1146,

		/// <summary>
		/// ER_NONEXISTING_TABLE_GRANT
		/// </summary>
		NonExistingTableGrant = 1147,

		/// <summary>
		/// ER_NOT_ALLOWED_COMMAND
		/// </summary>
		NotAllowedCommand = 1148,

		/// <summary>
		/// ER_SYNTAX_ERROR
		/// </summary>
		SyntaxError = 1149,

		/// <summary>
		/// ER_UNUSED1
		/// </summary>
		DelayedCannotChangeLock = 1150,

		/// <summary>
		/// ER_UNUSED2
		/// </summary>
		TooManyDelayedThreads = 1151,

		/// <summary>
		/// ER_ABORTING_CONNECTION
		/// </summary>
		AbortingConnection = 1152,

		/// <summary>
		/// ER_NET_PACKET_TOO_LARGE
		/// </summary>
		PacketTooLarge = 1153,

		/// <summary>
		/// ER_NET_READ_ERROR_FROM_PIPE
		/// </summary>
		NetReadErrorFromPipe = 1154,

		/// <summary>
		/// ER_NET_FCNTL_ERROR
		/// </summary>
		NetFCntlError = 1155,

		/// <summary>
		/// ER_NET_PACKETS_OUT_OF_ORDER
		/// </summary>
		NetPacketsOutOfOrder = 1156,

		/// <summary>
		/// ER_NET_UNCOMPRESS_ERROR
		/// </summary>
		NetUncompressError = 1157,

		/// <summary>
		/// ER_NET_READ_ERROR
		/// </summary>
		NetReadError = 1158,

		/// <summary>
		/// ER_NET_READ_INTERRUPTED
		/// </summary>
		NetReadInterrupted = 1159,

		/// <summary>
		/// ER_NET_ERROR_ON_WRITE
		/// </summary>
		NetErrorOnWrite = 1160,

		/// <summary>
		/// ER_NET_WRITE_INTERRUPTED
		/// </summary>
		NetWriteInterrupted = 1161,

		/// <summary>
		/// ER_TOO_LONG_STRING
		/// </summary>
		TooLongString = 1162,

		/// <summary>
		/// ER_TABLE_CANT_HANDLE_BLOB
		/// </summary>
		TableCannotHandleBlob = 1163,

		/// <summary>
		/// ER_TABLE_CANT_HANDLE_AUTO_INCREMENT
		/// </summary>
		TableCannotHandleAutoIncrement = 1164,

		/// <summary>
		/// ER_UNUSED3
		/// </summary>
		DelayedInsertTableLocked = 1165,

		/// <summary>
		/// ER_WRONG_COLUMN_NAME
		/// </summary>
		WrongColumnName = 1166,

		/// <summary>
		/// ER_WRONG_KEY_COLUMN
		/// </summary>
		WrongKeyColumn = 1167,

		/// <summary>
		/// ER_WRONG_MRG_TABLE
		/// </summary>
		WrongMergeTable = 1168,

		/// <summary>
		/// ER_DUP_UNIQUE
		/// </summary>
		DuplicateUnique = 1169,

		/// <summary>
		/// ER_BLOB_KEY_WITHOUT_LENGTH
		/// </summary>
		BlobKeyWithoutLength = 1170,

		/// <summary>
		/// ER_PRIMARY_CANT_HAVE_NULL
		/// </summary>
		PrimaryCannotHaveNull = 1171,

		/// <summary>
		/// ER_TOO_MANY_ROWS
		/// </summary>
		TooManyRows = 1172,

		/// <summary>
		/// ER_REQUIRES_PRIMARY_KEY
		/// </summary>
		RequiresPrimaryKey = 1173,

		/// <summary>
		/// ER_NO_RAID_COMPILED
		/// </summary>
		NoRAIDCompiled = 1174,

		/// <summary>
		/// ER_UPDATE_WITHOUT_KEY_IN_SAFE_MODE
		/// </summary>
		UpdateWithoutKeysInSafeMode = 1175,

		/// <summary>
		/// ER_KEY_DOES_NOT_EXITS
		/// </summary>
		KeyDoesNotExist = 1176,

		/// <summary>
		/// ER_CHECK_NO_SUCH_TABLE
		/// </summary>
		CheckNoSuchTable = 1177,

		/// <summary>
		/// ER_CHECK_NOT_IMPLEMENTED
		/// </summary>
		CheckNotImplemented = 1178,

		/// <summary>
		/// ER_CANT_DO_THIS_DURING_AN_TRANSACTION
		/// </summary>
		CannotDoThisDuringATransaction = 1179,

		/// <summary>
		/// ER_ERROR_DURING_COMMIT
		/// </summary>
		ErrorDuringCommit = 1180,

		/// <summary>
		/// ER_ERROR_DURING_ROLLBACK
		/// </summary>
		ErrorDuringRollback = 1181,

		/// <summary>
		/// ER_ERROR_DURING_FLUSH_LOGS
		/// </summary>
		ErrorDuringFlushLogs = 1182,

		/// <summary>
		/// ER_ERROR_DURING_CHECKPOINT
		/// </summary>
		ErrorDuringCheckpoint = 1183,

		/// <summary>
		/// ER_NEW_ABORTING_CONNECTION
		/// </summary>
		NewAbortingConnection = 1184,

		/// <summary>
		/// ER_DUMP_NOT_IMPLEMENTED
		/// </summary>
		DumpNotImplemented = 1185,

		/// <summary>
		/// ER_FLUSH_MASTER_BINLOG_CLOSED
		/// </summary>
		FlushMasterBinLogClosed = 1186,

		/// <summary>
		/// ER_INDEX_REBUILD
		/// </summary>
		IndexRebuild = 1187,

		/// <summary>
		/// ER_MASTER
		/// </summary>
		MasterError = 1188,

		/// <summary>
		/// ER_MASTER_NET_READ
		/// </summary>
		MasterNetRead = 1189,

		/// <summary>
		/// ER_MASTER_NET_WRITE
		/// </summary>
		MasterNetWrite = 1190,

		/// <summary>
		/// ER_FT_MATCHING_KEY_NOT_FOUND
		/// </summary>
		FullTextMatchingKeyNotFound = 1191,

		/// <summary>
		/// ER_LOCK_OR_ACTIVE_TRANSACTION
		/// </summary>
		LockOrActiveTransaction = 1192,

		/// <summary>
		/// ER_UNKNOWN_SYSTEM_VARIABLE
		/// </summary>
		UnknownSystemVariable = 1193,

		/// <summary>
		/// ER_CRASHED_ON_USAGE
		/// </summary>
		CrashedOnUsage = 1194,

		/// <summary>
		/// ER_CRASHED_ON_REPAIR
		/// </summary>
		CrashedOnRepair = 1195,

		/// <summary>
		/// ER_WARNING_NOT_COMPLETE_ROLLBACK
		/// </summary>
		WarningNotCompleteRollback = 1196,

		/// <summary>
		/// ER_TRANS_CACHE_FULL
		/// </summary>
		TransactionCacheFull = 1197,

		/// <summary>
		/// ER_SLAVE_MUST_STOP
		/// </summary>
		SlaveMustStop = 1198,

		/// <summary>
		/// ER_SLAVE_NOT_RUNNING
		/// </summary>
		SlaveNotRunning = 1199,

		/// <summary>
		/// ER_BAD_SLAVE
		/// </summary>
		BadSlave = 1200,

		/// <summary>
		/// ER_MASTER_INFO
		/// </summary>
		MasterInfo = 1201,

		/// <summary>
		/// ER_SLAVE_THREAD
		/// </summary>
		SlaveThread = 1202,

		/// <summary>
		/// ER_TOO_MANY_USER_CONNECTIONS
		/// </summary>
		TooManyUserConnections = 1203,

		/// <summary>
		/// ER_SET_CONSTANTS_ONLY
		/// </summary>
		SetConstantsOnly = 1204,

		/// <summary>
		/// ER_LOCK_WAIT_TIMEOUT
		/// </summary>
		LockWaitTimeout = 1205,

		/// <summary>
		/// ER_LOCK_TABLE_FULL
		/// </summary>
		LockTableFull = 1206,

		/// <summary>
		/// ER_READ_ONLY_TRANSACTION
		/// </summary>
		ReadOnlyTransaction = 1207,

		/// <summary>
		/// ER_DROP_DB_WITH_READ_LOCK
		/// </summary>
		DropDatabaseWithReadLock = 1208,

		/// <summary>
		/// ER_CREATE_DB_WITH_READ_LOCK
		/// </summary>
		CreateDatabaseWithReadLock = 1209,

		/// <summary>
		/// ER_WRONG_ARGUMENTS
		/// </summary>
		WrongArguments = 1210,

		/// <summary>
		/// ER_NO_PERMISSION_TO_CREATE_USER
		/// </summary>
		NoPermissionToCreateUser = 1211,

		/// <summary>
		/// ER_UNION_TABLES_IN_DIFFERENT_DIR
		/// </summary>
		UnionTablesInDifferentDirectory = 1212,

		/// <summary>
		/// ER_LOCK_DEADLOCK
		/// </summary>
		LockDeadlock = 1213,

		/// <summary>
		/// ER_TABLE_CANT_HANDLE_FT
		/// </summary>
		TableCannotHandleFullText = 1214,

		/// <summary>
		/// ER_CANNOT_ADD_FOREIGN
		/// </summary>
		CannotAddForeignConstraint = 1215,

		/// <summary>
		/// ER_NO_REFERENCED_ROW
		/// </summary>
		NoReferencedRow = 1216,

		/// <summary>
		/// ER_ROW_IS_REFERENCED
		/// </summary>
		RowIsReferenced = 1217,

		/// <summary>
		/// ER_CONNECT_TO_MASTER
		/// </summary>
		ConnectToMaster = 1218,

		/// <summary>
		/// ER_QUERY_ON_MASTER
		/// </summary>
		QueryOnMaster = 1219,

		/// <summary>
		/// ER_ERROR_WHEN_EXECUTING_COMMAND
		/// </summary>
		ErrorWhenExecutingCommand = 1220,

		/// <summary>
		/// ER_WRONG_USAGE
		/// </summary>
		WrongUsage = 1221,

		/// <summary>
		/// ER_WRONG_NUMBER_OF_COLUMNS_IN_SELECT
		/// </summary>
		WrongNumberOfColumnsInSelect = 1222,

		/// <summary>
		/// ER_CANT_UPDATE_WITH_READLOCK
		/// </summary>
		CannotUpdateWithReadLock = 1223,

		/// <summary>
		/// ER_MIXING_NOT_ALLOWED
		/// </summary>
		MixingNotAllowed = 1224,

		/// <summary>
		/// ER_DUP_ARGUMENT
		/// </summary>
		DuplicateArgument = 1225,

		/// <summary>
		/// ER_USER_LIMIT_REACHED
		/// </summary>
		UserLimitReached = 1226,

		/// <summary>
		/// ER_SPECIFIC_ACCESS_DENIED_ERROR
		/// </summary>
		SpecifiedAccessDeniedError = 1227,

		/// <summary>
		/// ER_LOCAL_VARIABLE
		/// </summary>
		LocalVariableError = 1228,

		/// <summary>
		/// ER_GLOBAL_VARIABLE
		/// </summary>
		GlobalVariableError = 1229,

		/// <summary>
		/// ER_NO_DEFAULT
		/// </summary>
		NotDefaultError = 1230,

		/// <summary>
		/// ER_WRONG_VALUE_FOR_VAR
		/// </summary>
		WrongValueForVariable = 1231,

		/// <summary>
		/// ER_WRONG_TYPE_FOR_VAR
		/// </summary>
		WrongTypeForVariable = 1232,

		/// <summary>
		/// ER_VAR_CANT_BE_READ
		/// </summary>
		VariableCannotBeRead = 1233,

		/// <summary>
		/// ER_CANT_USE_OPTION_HERE
		/// </summary>
		CannotUseOptionHere = 1234,

		/// <summary>
		/// ER_NOT_SUPPORTED_YET
		/// </summary>
		NotSupportedYet = 1235,

		/// <summary>
		/// ER_MASTER_FATAL_ERROR_READING_BINLOG
		/// </summary>
		MasterFatalErrorReadingBinLog = 1236,

		/// <summary>
		/// ER_SLAVE_IGNORED_TABLE
		/// </summary>
		SlaveIgnoredTable = 1237,

		/// <summary>
		/// ER_INCORRECT_GLOBAL_LOCAL_VAR
		/// </summary>
		IncorrectGlobalLocalVariable = 1238,

		/// <summary>
		/// ER_WRONG_FK_DEF
		/// </summary>
		WrongForeignKeyDefinition = 1239,

		/// <summary>
		/// ER_KEY_REF_DO_NOT_MATCH_TABLE_REF
		/// </summary>
		KeyReferenceDoesNotMatchTableReference = 1240,

		/// <summary>
		/// ER_OPERAND_COLUMNS
		/// </summary>
		OpearnColumnsError = 1241,

		/// <summary>
		/// ER_SUBQUERY_NO_1_ROW
		/// </summary>
		SubQueryNoOneRow = 1242,

		/// <summary>
		/// ER_UNKNOWN_STMT_HANDLER
		/// </summary>
		UnknownStatementHandler = 1243,

		/// <summary>
		/// ER_CORRUPT_HELP_DB
		/// </summary>
		CorruptHelpDatabase = 1244,

		/// <summary>
		/// ER_CYCLIC_REFERENCE
		/// </summary>
		CyclicReference = 1245,

		/// <summary>
		/// ER_AUTO_CONVERT
		/// </summary>
		AutoConvert = 1246,

		/// <summary>
		/// ER_ILLEGAL_REFERENCE
		/// </summary>
		IllegalReference = 1247,

		/// <summary>
		/// ER_DERIVED_MUST_HAVE_ALIAS
		/// </summary>
		DerivedMustHaveAlias = 1248,

		/// <summary>
		/// ER_SELECT_REDUCED
		/// </summary>
		SelectReduced = 1249,

		/// <summary>
		/// ER_TABLENAME_NOT_ALLOWED_HERE
		/// </summary>
		TableNameNotAllowedHere = 1250,

		/// <summary>
		/// ER_NOT_SUPPORTED_AUTH_MODE
		/// </summary>
		NotSupportedAuthMode = 1251,

		/// <summary>
		/// ER_SPATIAL_CANT_HAVE_NULL
		/// </summary>
		SpatialCannotHaveNull = 1252,

		/// <summary>
		/// ER_COLLATION_CHARSET_MISMATCH
		/// </summary>
		CollationCharsetMismatch = 1253,

		/// <summary>
		/// ER_SLAVE_WAS_RUNNING
		/// </summary>
		SlaveWasRunning = 1254,

		/// <summary>
		/// ER_SLAVE_WAS_NOT_RUNNING
		/// </summary>
		SlaveWasNotRunning = 1255,

		/// <summary>
		/// ER_TOO_BIG_FOR_UNCOMPRESS
		/// </summary>
		TooBigForUncompress = 1256,

		/// <summary>
		/// ER_ZLIB_Z_MEM_ERROR
		/// </summary>
		ZipLibMemoryError = 1257,

		/// <summary>
		/// ER_ZLIB_Z_BUF_ERROR
		/// </summary>
		ZipLibBufferError = 1258,

		/// <summary>
		/// ER_ZLIB_Z_DATA_ERROR
		/// </summary>
		ZipLibDataError = 1259,

		/// <summary>
		/// ER_CUT_VALUE_GROUP_CONCAT
		/// </summary>
		CutValueGroupConcat = 1260,

		/// <summary>
		/// ER_WARN_TOO_FEW_RECORDS
		/// </summary>
		WarningTooFewRecords = 1261,

		/// <summary>
		/// ER_WARN_TOO_MANY_RECORDS
		/// </summary>
		WarningTooManyRecords = 1262,

		/// <summary>
		/// ER_WARN_NULL_TO_NOTNULL
		/// </summary>
		WarningNullToNotNull = 1263,

		/// <summary>
		/// ER_WARN_DATA_OUT_OF_RANGE
		/// </summary>
		WarningDataOutOfRange = 1264,

		/// <summary>
		/// WARN_DATA_TRUNCATED
		/// </summary>
		WaningDataTruncated = 1265,

		/// <summary>
		/// ER_WARN_USING_OTHER_HANDLER
		/// </summary>
		WaningUsingOtherHandler = 1266,

		/// <summary>
		/// ER_CANT_AGGREGATE_2COLLATIONS
		/// </summary>
		CannotAggregateTwoCollations = 1267,

		/// <summary>
		/// ER_DROP_USER
		/// </summary>
		DropUserError = 1268,

		/// <summary>
		/// ER_REVOKE_GRANTS
		/// </summary>
		RevokeGrantsError = 1269,

		/// <summary>
		/// ER_CANT_AGGREGATE_3COLLATIONS
		/// </summary>
		CannotAggregateThreeCollations = 1270,

		/// <summary>
		/// ER_CANT_AGGREGATE_NCOLLATIONS
		/// </summary>
		CannotAggregateNCollations = 1271,

		/// <summary>
		/// ER_VARIABLE_IS_NOT_STRUCT
		/// </summary>
		VariableIsNotStructure = 1272,

		/// <summary>
		/// ER_UNKNOWN_COLLATION
		/// </summary>
		UnknownCollation = 1273,

		/// <summary>
		/// ER_SLAVE_IGNORED_SSL_PARAMS
		/// </summary>
		SlaveIgnoreSSLParameters = 1274,

		/// <summary>
		/// ER_SERVER_IS_IN_SECURE_AUTH_MODE
		/// </summary>
		ServerIsInSecureAuthMode = 1275,

		/// <summary>
		/// ER_WARN_FIELD_RESOLVED
		/// </summary>
		WaningFieldResolved = 1276,

		/// <summary>
		/// ER_BAD_SLAVE_UNTIL_COND
		/// </summary>
		BadSlaveUntilCondition = 1277,

		/// <summary>
		/// ER_MISSING_SKIP_SLAVE
		/// </summary>
		MissingSkipSlave = 1278,

		/// <summary>
		/// ER_UNTIL_COND_IGNORED
		/// </summary>
		ErrorUntilConditionIgnored = 1279,

		/// <summary>
		/// ER_WRONG_NAME_FOR_INDEX
		/// </summary>
		WrongNameForIndex = 1280,

		/// <summary>
		/// ER_WRONG_NAME_FOR_CATALOG
		/// </summary>
		WrongNameForCatalog = 1281,

		/// <summary>
		/// ER_WARN_QC_RESIZE
		/// </summary>
		WarningQueryCacheResize = 1282,

		/// <summary>
		/// ER_BAD_FT_COLUMN
		/// </summary>
		BadFullTextColumn = 1283,

		/// <summary>
		/// ER_UNKNOWN_KEY_CACHE
		/// </summary>
		UnknownKeyCache = 1284,

		/// <summary>
		/// ER_WARN_HOSTNAME_WONT_WORK
		/// </summary>
		WarningHostnameWillNotWork = 1285,

		/// <summary>
		/// ER_UNKNOWN_STORAGE_ENGINE
		/// </summary>
		UnknownStorageEngine = 1286,

		/// <summary>
		/// ER_WARN_DEPRECATED_SYNTAX
		/// </summary>
		WaningDeprecatedSyntax = 1287,

		/// <summary>
		/// ER_NON_UPDATABLE_TABLE
		/// </summary>
		NonUpdateableTable = 1288,

		/// <summary>
		/// ER_FEATURE_DISABLED
		/// </summary>
		FeatureDisabled = 1289,

		/// <summary>
		/// ER_OPTION_PREVENTS_STATEMENT
		/// </summary>
		OptionPreventsStatement = 1290,

		/// <summary>
		/// ER_DUPLICATED_VALUE_IN_TYPE
		/// </summary>
		DuplicatedValueInType = 1291,

		/// <summary>
		/// ER_TRUNCATED_WRONG_VALUE
		/// </summary>
		TruncatedWrongValue = 1292,

		/// <summary>
		/// ER_TOO_MUCH_AUTO_TIMESTAMP_COLS
		/// </summary>
		TooMuchAutoTimestampColumns = 1293,

		/// <summary>
		/// ER_INVALID_ON_UPDATE
		/// </summary>
		InvalidOnUpdate = 1294,

		/// <summary>
		/// ER_UNSUPPORTED_PS
		/// </summary>
		UnsupportedPreparedStatement = 1295,

		/// <summary>
		/// ER_GET_ERRMSG
		/// </summary>
		GetErroMessage = 1296,

		/// <summary>
		/// ER_GET_TEMPORARY_ERRMSG
		/// </summary>
		GetTemporaryErrorMessage = 1297,

		/// <summary>
		/// ER_UNKNOWN_TIME_ZONE
		/// </summary>
		UnknownTimeZone = 1298,

		/// <summary>
		/// ER_WARN_INVALID_TIMESTAMP
		/// </summary>
		WarningInvalidTimestamp = 1299,

		/// <summary>
		/// ER_INVALID_CHARACTER_STRING
		/// </summary>
		InvalidCharacterString = 1300,

		/// <summary>
		/// ER_WARN_ALLOWED_PACKET_OVERFLOWED
		/// </summary>
		WarningAllowedPacketOverflowed = 1301,

		/// <summary>
		/// ER_CONFLICTING_DECLARATIONS
		/// </summary>
		ConflictingDeclarations = 1302,

		/// <summary>
		/// ER_SP_NO_RECURSIVE_CREATE
		/// </summary>
		StoredProcedureNoRecursiveCreate = 1303,

		/// <summary>
		/// ER_SP_ALREADY_EXISTS
		/// </summary>
		StoredProcedureAlreadyExists = 1304,

		/// <summary>
		/// ER_SP_DOES_NOT_EXIST
		/// </summary>
		StoredProcedureDoesNotExist = 1305,

		/// <summary>
		/// ER_SP_DROP_FAILED
		/// </summary>
		StoredProcedureDropFailed = 1306,

		/// <summary>
		/// ER_SP_STORE_FAILED
		/// </summary>
		StoredProcedureStoreFailed = 1307,

		/// <summary>
		/// ER_SP_LILABEL_MISMATCH
		/// </summary>
		StoredProcedureLiLabelMismatch = 1308,

		/// <summary>
		/// ER_SP_LABEL_REDEFINE
		/// </summary>
		StoredProcedureLabelRedefine = 1309,

		/// <summary>
		/// ER_SP_LABEL_MISMATCH
		/// </summary>
		StoredProcedureLabelMismatch = 1310,

		/// <summary>
		/// ER_SP_UNINIT_VAR
		/// </summary>
		StoredProcedureUninitializedVariable = 1311,

		/// <summary>
		/// ER_SP_BADSELECT
		/// </summary>
		StoredProcedureBadSelect = 1312,

		/// <summary>
		/// ER_SP_BADRETURN
		/// </summary>
		StoredProcedureBadReturn = 1313,

		/// <summary>
		/// ER_SP_BADSTATEMENT
		/// </summary>
		StoredProcedureBadStatement = 1314,

		/// <summary>
		/// ER_UPDATE_LOG_DEPRECATED_IGNORED
		/// </summary>
		UpdateLogDeprecatedIgnored = 1315,

		/// <summary>
		/// ER_UPDATE_LOG_DEPRECATED_TRANSLATED
		/// </summary>
		UpdateLogDeprecatedTranslated = 1316,

		/// <summary>
		/// Query execution was interrupted (ER_QUERY_INTERRUPTED).
		/// </summary>
		QueryInterrupted = 1317,

		/// <summary>
		/// ER_SP_WRONG_NO_OF_ARGS
		/// </summary>
		StoredProcedureNumberOfArguments = 1318,

		/// <summary>
		/// ER_SP_COND_MISMATCH
		/// </summary>
		StoredProcedureConditionMismatch = 1319,

		/// <summary>
		/// ER_SP_NORETURN
		/// </summary>
		StoredProcedureNoReturn = 1320,

		/// <summary>
		/// ER_SP_NORETURNEND
		/// </summary>
		StoredProcedureNoReturnEnd = 1321,

		/// <summary>
		/// ER_SP_BAD_CURSOR_QUERY
		/// </summary>
		StoredProcedureBadCursorQuery = 1322,

		/// <summary>
		/// ER_SP_BAD_CURSOR_SELECT
		/// </summary>
		StoredProcedureBadCursorSelect = 1323,

		/// <summary>
		/// ER_SP_CURSOR_MISMATCH
		/// </summary>
		StoredProcedureCursorMismatch = 1324,

		/// <summary>
		/// ER_SP_CURSOR_ALREADY_OPEN
		/// </summary>
		StoredProcedureAlreadyOpen = 1325,

		/// <summary>
		/// ER_SP_CURSOR_NOT_OPEN
		/// </summary>
		StoredProcedureCursorNotOpen = 1326,

		/// <summary>
		/// ER_SP_UNDECLARED_VAR
		/// </summary>
		StoredProcedureUndeclaredVariabel = 1327,

		/// <summary>
		/// ER_SP_WRONG_NO_OF_FETCH_ARGS
		/// </summary>
		StoredProcedureWrongNumberOfFetchArguments = 1328,

		/// <summary>
		/// ER_SP_FETCH_NO_DATA
		/// </summary>
		StoredProcedureFetchNoData = 1329,

		/// <summary>
		/// ER_SP_DUP_PARAM
		/// </summary>
		StoredProcedureDuplicateParameter = 1330,

		/// <summary>
		/// ER_SP_DUP_VAR
		/// </summary>
		StoredProcedureDuplicateVariable = 1331,

		/// <summary>
		/// ER_SP_DUP_COND
		/// </summary>
		StoredProcedureDuplicateCondition = 1332,

		/// <summary>
		/// ER_SP_DUP_CURS
		/// </summary>
		StoredProcedureDuplicateCursor = 1333,

		/// <summary>
		/// ER_SP_CANT_ALTER
		/// </summary>
		StoredProcedureCannotAlter = 1334,

		/// <summary>
		/// ER_SP_SUBSELECT_NYI
		/// </summary>
		StoredProcedureSubSelectNYI = 1335,

		/// <summary>
		/// ER_STMT_NOT_ALLOWED_IN_SF_OR_TRG
		/// </summary>
		StatementNotAllowedInStoredFunctionOrTrigger = 1336,

		/// <summary>
		/// ER_SP_VARCOND_AFTER_CURSHNDLR
		/// </summary>
		StoredProcedureVariableConditionAfterCursorHandler = 1337,

		/// <summary>
		/// ER_SP_CURSOR_AFTER_HANDLER
		/// </summary>
		StoredProcedureCursorAfterHandler = 1338,

		/// <summary>
		/// ER_SP_CASE_NOT_FOUND
		/// </summary>
		StoredProcedureCaseNotFound = 1339,

		/// <summary>
		/// ER_FPARSER_TOO_BIG_FILE
		/// </summary>
		FileParserTooBigFile = 1340,

		/// <summary>
		/// ER_FPARSER_BAD_HEADER
		/// </summary>
		FileParserBadHeader = 1341,

		/// <summary>
		/// ER_FPARSER_EOF_IN_COMMENT
		/// </summary>
		FileParserEOFInComment = 1342,

		/// <summary>
		/// ER_FPARSER_ERROR_IN_PARAMETER
		/// </summary>
		FileParserErrorInParameter = 1343,

		/// <summary>
		/// ER_FPARSER_EOF_IN_UNKNOWN_PARAMETER
		/// </summary>
		FileParserEOFInUnknownParameter = 1344,

		/// <summary>
		/// ER_VIEW_NO_EXPLAIN
		/// </summary>
		ViewNoExplain = 1345,

		/// <summary>
		/// ER_FRM_UNKNOWN_TYPE
		/// </summary>
		FrmUnknownType = 1346,

		/// <summary>
		/// ER_WRONG_OBJECT
		/// </summary>
		WrongObject = 1347,

		/// <summary>
		/// ER_NONUPDATEABLE_COLUMN
		/// </summary>
		NonUpdateableColumn = 1348,

		/// <summary>
		/// ER_VIEW_SELECT_DERIVED
		/// </summary>
		ViewSelectDerived = 1349,

		/// <summary>
		/// ER_VIEW_SELECT_CLAUSE
		/// </summary>
		ViewSelectClause = 1350,

		/// <summary>
		/// ER_VIEW_SELECT_VARIABLE
		/// </summary>
		ViewSelectVariable = 1351,

		/// <summary>
		/// ER_VIEW_SELECT_TMPTABLE
		/// </summary>
		ViewSelectTempTable = 1352,

		/// <summary>
		/// ER_VIEW_WRONG_LIST
		/// </summary>
		ViewWrongList = 1353,

		/// <summary>
		/// ER_WARN_VIEW_MERGE
		/// </summary>
		WarningViewMerge = 1354,

		/// <summary>
		/// ER_WARN_VIEW_WITHOUT_KEY
		/// </summary>
		WarningViewWithoutKey = 1355,

		/// <summary>
		/// ER_VIEW_INVALID
		/// </summary>
		ViewInvalid = 1356,

		/// <summary>
		/// ER_SP_NO_DROP_SP
		/// </summary>
		StoredProcedureNoDropStoredProcedure = 1357,

		/// <summary>
		/// ER_SP_GOTO_IN_HNDLR
		/// </summary>
		StoredProcedureGotoInHandler = 1358,

		/// <summary>
		/// ER_TRG_ALREADY_EXISTS
		/// </summary>
		TriggerAlreadyExists = 1359,

		/// <summary>
		/// ER_TRG_DOES_NOT_EXIST
		/// </summary>
		TriggerDoesNotExist = 1360,

		/// <summary>
		/// ER_TRG_ON_VIEW_OR_TEMP_TABLE
		/// </summary>
		TriggerOnViewOrTempTable = 1361,

		/// <summary>
		/// ER_TRG_CANT_CHANGE_ROW
		/// </summary>
		TriggerCannotChangeRow = 1362,

		/// <summary>
		/// ER_TRG_NO_SUCH_ROW_IN_TRG
		/// </summary>
		TriggerNoSuchRowInTrigger = 1363,

		/// <summary>
		/// ER_NO_DEFAULT_FOR_FIELD
		/// </summary>
		NoDefaultForField = 1364,

		/// <summary>
		/// ER_DIVISION_BY_ZERO
		/// </summary>
		DivisionByZero = 1365,

		/// <summary>
		/// ER_TRUNCATED_WRONG_VALUE_FOR_FIELD
		/// </summary>
		TruncatedWrongValueForField = 1366,

		/// <summary>
		/// ER_ILLEGAL_VALUE_FOR_TYPE
		/// </summary>
		IllegalValueForType = 1367,

		/// <summary>
		/// ER_VIEW_NONUPD_CHECK
		/// </summary>
		ViewNonUpdatableCheck = 1368,

		/// <summary>
		/// ER_VIEW_CHECK_FAILED
		/// </summary>
		ViewCheckFailed = 1369,

		/// <summary>
		/// ER_PROCACCESS_DENIED_ERROR
		/// </summary>
		PrecedureAccessDenied = 1370,

		/// <summary>
		/// ER_RELAY_LOG_FAIL
		/// </summary>
		RelayLogFail = 1371,

		/// <summary>
		/// ER_PASSWD_LENGTH
		/// </summary>
		PasswordLength = 1372,

		/// <summary>
		/// ER_UNKNOWN_TARGET_BINLOG
		/// </summary>
		UnknownTargetBinLog = 1373,

		/// <summary>
		/// ER_IO_ERR_LOG_INDEX_READ
		/// </summary>
		IOErrorLogIndexRead = 1374,

		/// <summary>
		/// ER_BINLOG_PURGE_PROHIBITED
		/// </summary>
		BinLogPurgeProhibited = 1375,

		/// <summary>
		/// ER_FSEEK_FAIL
		/// </summary>
		FSeekFail = 1376,

		/// <summary>
		/// ER_BINLOG_PURGE_FATAL_ERR
		/// </summary>
		BinLogPurgeFatalError = 1377,

		/// <summary>
		/// ER_LOG_IN_USE
		/// </summary>
		LogInUse = 1378,

		/// <summary>
		/// ER_LOG_PURGE_UNKNOWN_ERR
		/// </summary>
		LogPurgeUnknownError = 1379,

		/// <summary>
		/// ER_RELAY_LOG_INIT
		/// </summary>
		RelayLogInit = 1380,

		/// <summary>
		/// ER_NO_BINARY_LOGGING
		/// </summary>
		NoBinaryLogging = 1381,

		/// <summary>
		/// ER_RESERVED_SYNTAX
		/// </summary>
		ReservedSyntax = 1382,

		/// <summary>
		/// ER_WSAS_FAILED
		/// </summary>
		WSAStartupFailed = 1383,

		/// <summary>
		/// ER_DIFF_GROUPS_PROC
		/// </summary>
		DifferentGroupsProcedure = 1384,

		/// <summary>
		/// ER_NO_GROUP_FOR_PROC
		/// </summary>
		NoGroupForProcedure = 1385,

		/// <summary>
		/// ER_ORDER_WITH_PROC
		/// </summary>
		OrderWithProcedure = 1386,

		/// <summary>
		/// ER_LOGGING_PROHIBIT_CHANGING_OF
		/// </summary>
		LoggingProhibitsChangingOf = 1387,

		/// <summary>
		/// ER_NO_FILE_MAPPING
		/// </summary>
		NoFileMapping = 1388,

		/// <summary>
		/// ER_WRONG_MAGIC
		/// </summary>
		WrongMagic = 1389,

		/// <summary>
		/// ER_PS_MANY_PARAM
		/// </summary>
		PreparedStatementManyParameters = 1390,

		/// <summary>
		/// ER_KEY_PART_0
		/// </summary>
		KeyPartZero = 1391,

		/// <summary>
		/// ER_VIEW_CHECKSUM
		/// </summary>
		ViewChecksum = 1392,

		/// <summary>
		/// ER_VIEW_MULTIUPDATE
		/// </summary>
		ViewMultiUpdate = 1393,

		/// <summary>
		/// ER_VIEW_NO_INSERT_FIELD_LIST
		/// </summary>
		ViewNoInsertFieldList = 1394,

		/// <summary>
		/// ER_VIEW_DELETE_MERGE_VIEW
		/// </summary>
		ViewDeleteMergeView = 1395,

		/// <summary>
		/// ER_CANNOT_USER
		/// </summary>
		CannotUser = 1396,

		/// <summary>
		/// ER_XAER_NOTA
		/// </summary>
		XAERNotA = 1397,

		/// <summary>
		/// ER_XAER_INVAL
		/// </summary>
		XAERInvalid = 1398,

		/// <summary>
		/// ER_XAER_RMFAIL
		/// </summary>
		XAERRemoveFail = 1399,

		/// <summary>
		/// ER_XAER_OUTSIDE
		/// </summary>
		XAEROutside = 1400,

		/// <summary>
		/// ER_XAER_RMERR
		/// </summary>
		XAERRemoveError = 1401,

		/// <summary>
		/// ER_XA_RBROLLBACK
		/// </summary>
		XARBRollback = 1402,

		/// <summary>
		/// ER_NONEXISTING_PROC_GRANT
		/// </summary>
		NonExistingProcedureGrant = 1403,

		/// <summary>
		/// ER_PROC_AUTO_GRANT_FAIL
		/// </summary>
		ProcedureAutoGrantFail = 1404,

		/// <summary>
		/// ER_PROC_AUTO_REVOKE_FAIL
		/// </summary>
		ProcedureAutoRevokeFail = 1405,

		/// <summary>
		/// ER_DATA_TOO_LONG
		/// </summary>
		DataTooLong = 1406,

		/// <summary>
		/// ER_SP_BAD_SQLSTATE
		/// </summary>
		StoredProcedureSQLState = 1407,

		/// <summary>
		/// ER_STARTUP
		/// </summary>
		StartupError = 1408,

		/// <summary>
		/// ER_LOAD_FROM_FIXED_SIZE_ROWS_TO_VAR
		/// </summary>
		LoadFromFixedSizeRowsToVariable = 1409,

		/// <summary>
		/// ER_CANT_CREATE_USER_WITH_GRANT
		/// </summary>
		CannotCreateUserWithGrant = 1410,

		/// <summary>
		/// ER_WRONG_VALUE_FOR_TYPE
		/// </summary>
		WrongValueForType = 1411,

		/// <summary>
		/// ER_TABLE_DEF_CHANGED
		/// </summary>
		TableDefinitionChanged = 1412,

		/// <summary>
		/// ER_SP_DUP_HANDLER
		/// </summary>
		StoredProcedureDuplicateHandler = 1413,

		/// <summary>
		/// ER_SP_NOT_VAR_ARG
		/// </summary>
		StoredProcedureNotVariableArgument = 1414,

		/// <summary>
		/// ER_SP_NO_RETSET
		/// </summary>
		StoredProcedureNoReturnSet = 1415,

		/// <summary>
		/// ER_CANT_CREATE_GEOMETRY_OBJECT
		/// </summary>
		CannotCreateGeometryObject = 1416,

		/// <summary>
		/// ER_FAILED_ROUTINE_BREAK_BINLOG
		/// </summary>
		FailedRoutineBreaksBinLog = 1417,

		/// <summary>
		/// ER_BINLOG_UNSAFE_ROUTINE
		/// </summary>
		BinLogUnsafeRoutine = 1418,

		/// <summary>
		/// ER_BINLOG_CREATE_ROUTINE_NEED_SUPER
		/// </summary>
		BinLogCreateRoutineNeedSuper = 1419,

		/// <summary>
		/// ER_EXEC_STMT_WITH_OPEN_CURSOR
		/// </summary>
		ExecuteStatementWithOpenCursor = 1420,

		/// <summary>
		/// ER_STMT_HAS_NO_OPEN_CURSOR
		/// </summary>
		StatementHasNoOpenCursor = 1421,

		/// <summary>
		/// ER_COMMIT_NOT_ALLOWED_IN_SF_OR_TRG
		/// </summary>
		CommitNotAllowedIfStoredFunctionOrTrigger = 1422,

		/// <summary>
		/// ER_NO_DEFAULT_FOR_VIEW_FIELD
		/// </summary>
		NoDefaultForViewField = 1423,

		/// <summary>
		/// ER_SP_NO_RECURSION
		/// </summary>
		StoredProcedureNoRecursion = 1424,

		/// <summary>
		/// ER_TOO_BIG_SCALE
		/// </summary>
		TooBigScale = 1425,

		/// <summary>
		/// ER_TOO_BIG_PRECISION
		/// </summary>
		TooBigPrecision = 1426,

		/// <summary>
		/// ER_M_BIGGER_THAN_D
		/// </summary>
		MBiggerThanD = 1427,

		/// <summary>
		/// ER_WRONG_LOCK_OF_SYSTEM_TABLE
		/// </summary>
		WrongLockOfSystemTable = 1428,

		/// <summary>
		/// ER_CONNECT_TO_FOREIGN_DATA_SOURCE
		/// </summary>
		ConnectToForeignDataSource = 1429,

		/// <summary>
		/// ER_QUERY_ON_FOREIGN_DATA_SOURCE
		/// </summary>
		QueryOnForeignDataSource = 1430,

		/// <summary>
		/// ER_FOREIGN_DATA_SOURCE_DOESNT_EXIST
		/// </summary>
		ForeignDataSourceDoesNotExist = 1431,

		/// <summary>
		/// ER_FOREIGN_DATA_STRING_INVALID_CANT_CREATE
		/// </summary>
		ForeignDataStringInvalidCannotCreate = 1432,

		/// <summary>
		/// ER_FOREIGN_DATA_STRING_INVALID
		/// </summary>
		ForeignDataStringInvalid = 1433,

		/// <summary>
		/// ER_CANT_CREATE_FEDERATED_TABLE
		/// </summary>
		CannotCreateFederatedTable = 1434,

		/// <summary>
		/// ER_TRG_IN_WRONG_SCHEMA
		/// </summary>
		TriggerInWrongSchema = 1435,

		/// <summary>
		/// ER_STACK_OVERRUN_NEED_MORE
		/// </summary>
		StackOverrunNeedMore = 1436,

		/// <summary>
		/// ER_TOO_LONG_BODY
		/// </summary>
		TooLongBody = 1437,

		/// <summary>
		/// ER_WARN_CANT_DROP_DEFAULT_KEYCACHE
		/// </summary>
		WarningCannotDropDefaultKeyCache = 1438,

		/// <summary>
		/// ER_TOO_BIG_DISPLAYWIDTH
		/// </summary>
		TooBigDisplayWidth = 1439,

		/// <summary>
		/// ER_XAER_DUPID
		/// </summary>
		XAERDuplicateID = 1440,

		/// <summary>
		/// ER_DATETIME_FUNCTION_OVERFLOW
		/// </summary>
		DateTimeFunctionOverflow = 1441,

		/// <summary>
		/// ER_CANT_UPDATE_USED_TABLE_IN_SF_OR_TRG
		/// </summary>
		CannotUpdateUsedTableInStoredFunctionOrTrigger = 1442,

		/// <summary>
		/// ER_VIEW_PREVENT_UPDATE
		/// </summary>
		ViewPreventUpdate = 1443,

		/// <summary>
		/// ER_PS_NO_RECURSION
		/// </summary>
		PreparedStatementNoRecursion = 1444,

		/// <summary>
		/// ER_SP_CANT_SET_AUTOCOMMIT
		/// </summary>
		StoredProcedureCannotSetAutoCommit = 1445,

		/// <summary>
		/// ER_MALFORMED_DEFINER
		/// </summary>
		MalformedDefiner = 1446,

		/// <summary>
		/// ER_VIEW_FRM_NO_USER
		/// </summary>
		ViewFrmNoUser = 1447,

		/// <summary>
		/// ER_VIEW_OTHER_USER
		/// </summary>
		ViewOtherUser = 1448,

		/// <summary>
		/// ER_NO_SUCH_USER
		/// </summary>
		NoSuchUser = 1449,

		/// <summary>
		/// ER_FORBID_SCHEMA_CHANGE
		/// </summary>
		ForbidSchemaChange = 1450,

		/// <summary>
		/// ER_ROW_IS_REFERENCED_2
		/// </summary>
		RowIsReferenced2 = 1451,

		/// <summary>
		/// ER_NO_REFERENCED_ROW_2
		/// </summary>
		NoReferencedRow2 = 1452,

		/// <summary>
		/// ER_SP_BAD_VAR_SHADOW
		/// </summary>
		StoredProcedureBadVariableShadow = 1453,

		/// <summary>
		/// ER_TRG_NO_DEFINER
		/// </summary>
		TriggerNoDefiner = 1454,

		/// <summary>
		/// ER_OLD_FILE_FORMAT
		/// </summary>
		OldFileFormat = 1455,

		/// <summary>
		/// ER_SP_RECURSION_LIMIT
		/// </summary>
		StoredProcedureRecursionLimit = 1456,

		/// <summary>
		/// ER_SP_PROC_TABLE_CORRUPT
		/// </summary>
		StoredProcedureTableCorrupt = 1457,

		/// <summary>
		/// ER_SP_WRONG_NAME
		/// </summary>
		StoredProcedureWrongName = 1458,

		/// <summary>
		/// ER_TABLE_NEEDS_UPGRADE
		/// </summary>
		TableNeedsUpgrade = 1459,

		/// <summary>
		/// ER_SP_NO_AGGREGATE
		/// </summary>
		StoredProcedureNoAggregate = 1460,

		/// <summary>
		/// ER_MAX_PREPARED_STMT_COUNT_REACHED
		/// </summary>
		MaxPreparedStatementCountReached = 1461,

		/// <summary>
		/// ER_VIEW_RECURSIVE
		/// </summary>
		ViewRecursive = 1462,

		/// <summary>
		/// ER_NON_GROUPING_FIELD_USED
		/// </summary>
		NonGroupingFieldUsed = 1463,

		/// <summary>
		/// ER_TABLE_CANT_HANDLE_SPKEYS
		/// </summary>
		TableCannotHandleSpatialKeys = 1464,

		/// <summary>
		/// ER_NO_TRIGGERS_ON_SYSTEM_SCHEMA
		/// </summary>
		NoTriggersOnSystemSchema = 1465,

		/// <summary>
		/// ER_REMOVED_SPACES
		/// </summary>
		RemovedSpaces = 1466,

		/// <summary>
		/// ER_AUTOINC_READ_FAILED
		/// </summary>
		AutoIncrementReadFailed = 1467,

		/// <summary>
		/// ER_USERNAME
		/// </summary>
		UserNameError = 1468,

		/// <summary>
		/// ER_HOSTNAME
		/// </summary>
		HostNameError = 1469,

		/// <summary>
		/// ER_WRONG_STRING_LENGTH
		/// </summary>
		WrongStringLength = 1470,

		/// <summary>
		/// ER_NON_INSERTABLE_TABLE
		/// </summary>
		NonInsertableTable = 1471,

		/// <summary>
		/// ER_ADMIN_WRONG_MRG_TABLE
		/// </summary>
		AdminWrongMergeTable = 1472,

		/// <summary>
		/// ER_TOO_HIGH_LEVEL_OF_NESTING_FOR_SELECT
		/// </summary>
		TooHighLevelOfNestingForSelect = 1473,

		/// <summary>
		/// ER_NAME_BECOMES_EMPTY
		/// </summary>
		NameBecomesEmpty = 1474,

		/// <summary>
		/// ER_AMBIGUOUS_FIELD_TERM
		/// </summary>
		AmbiguousFieldTerm = 1475,

		/// <summary>
		/// ER_FOREIGN_SERVER_EXISTS
		/// </summary>
		ForeignServerExists = 1476,

		/// <summary>
		/// ER_FOREIGN_SERVER_DOESNT_EXIST
		/// </summary>
		ForeignServerDoesNotExist = 1477,

		/// <summary>
		/// ER_ILLEGAL_HA_CREATE_OPTION
		/// </summary>
		IllegalHACreateOption = 1478,

		/// <summary>
		/// ER_PARTITION_REQUIRES_VALUES_ERROR
		/// </summary>
		PartitionRequiresValues = 1479,

		/// <summary>
		/// ER_PARTITION_WRONG_VALUES_ERROR
		/// </summary>
		PartitionWrongValues = 1480,

		/// <summary>
		/// ER_PARTITION_MAXVALUE_ERROR
		/// </summary>
		PartitionMaxValue = 1481,

		/// <summary>
		/// ER_PARTITION_SUBPARTITION_ERROR
		/// </summary>
		PartitionSubPartition = 1482,

		/// <summary>
		/// ER_PARTITION_SUBPART_MIX_ERROR
		/// </summary>
		PartitionSubPartMix = 1483,

		/// <summary>
		/// ER_PARTITION_WRONG_NO_PART_ERROR
		/// </summary>
		PartitionWrongNoPart = 1484,

		/// <summary>
		/// ER_PARTITION_WRONG_NO_SUBPART_ERROR
		/// </summary>
		PartitionWrongNoSubPart = 1485,

		/// <summary>
		/// ER_WRONG_EXPR_IN_PARTITION_FUNC_ERROR
		/// </summary>
		WrongExpressionInParitionFunction = 1486,

		/// <summary>
		/// ER_NO_CONST_EXPR_IN_RANGE_OR_LIST_ERROR
		/// </summary>
		NoConstantExpressionInRangeOrListError = 1487,

		/// <summary>
		/// ER_FIELD_NOT_FOUND_PART_ERROR
		/// </summary>
		FieldNotFoundPartitionErrror = 1488,

		/// <summary>
		/// ER_LIST_OF_FIELDS_ONLY_IN_HASH_ERROR
		/// </summary>
		ListOfFieldsOnlyInHash = 1489,

		/// <summary>
		/// ER_INCONSISTENT_PARTITION_INFO_ERROR
		/// </summary>
		InconsistentPartitionInfo = 1490,

		/// <summary>
		/// ER_PARTITION_FUNC_NOT_ALLOWED_ERROR
		/// </summary>
		PartitionFunctionNotAllowed = 1491,

		/// <summary>
		/// ER_PARTITIONS_MUST_BE_DEFINED_ERROR
		/// </summary>
		PartitionsMustBeDefined = 1492,

		/// <summary>
		/// ER_RANGE_NOT_INCREASING_ERROR
		/// </summary>
		RangeNotIncreasing = 1493,

		/// <summary>
		/// ER_INCONSISTENT_TYPE_OF_FUNCTIONS_ERROR
		/// </summary>
		InconsistentTypeOfFunctions = 1494,

		/// <summary>
		/// ER_MULTIPLE_DEF_CONST_IN_LIST_PART_ERROR
		/// </summary>
		MultipleDefinitionsConstantInListPartition = 1495,

		/// <summary>
		/// ER_PARTITION_ENTRY_ERROR
		/// </summary>
		PartitionEntryError = 1496,

		/// <summary>
		/// ER_MIX_HANDLER_ERROR
		/// </summary>
		MixHandlerError = 1497,

		/// <summary>
		/// ER_PARTITION_NOT_DEFINED_ERROR
		/// </summary>
		PartitionNotDefined = 1498,

		/// <summary>
		/// ER_TOO_MANY_PARTITIONS_ERROR
		/// </summary>
		TooManyPartitions = 1499,

		/// <summary>
		/// ER_SUBPARTITION_ERROR
		/// </summary>
		SubPartitionError = 1500,

		/// <summary>
		/// ER_CANT_CREATE_HANDLER_FILE
		/// </summary>
		CannotCreateHandlerFile = 1501,

		/// <summary>
		/// ER_BLOB_FIELD_IN_PART_FUNC_ERROR
		/// </summary>
		BlobFieldInPartitionFunction = 1502,

		/// <summary>
		/// ER_UNIQUE_KEY_NEED_ALL_FIELDS_IN_PF
		/// </summary>
		UniqueKeyNeedAllFieldsInPartitioningFunction = 1503,

		/// <summary>
		/// ER_NO_PARTS_ERROR
		/// </summary>
		NoPartitions = 1504,

		/// <summary>
		/// ER_PARTITION_MGMT_ON_NONPARTITIONED
		/// </summary>
		PartitionManagementOnNoPartitioned = 1505,

		/// <summary>
		/// ER_FOREIGN_KEY_ON_PARTITIONED
		/// </summary>
		ForeignKeyOnPartitioned = 1506,

		/// <summary>
		/// ER_DROP_PARTITION_NON_EXISTENT
		/// </summary>
		DropPartitionNonExistent = 1507,

		/// <summary>
		/// ER_DROP_LAST_PARTITION
		/// </summary>
		DropLastPartition = 1508,

		/// <summary>
		/// ER_COALESCE_ONLY_ON_HASH_PARTITION
		/// </summary>
		CoalesceOnlyOnHashPartition = 1509,

		/// <summary>
		/// ER_REORG_HASH_ONLY_ON_SAME_NO
		/// </summary>
		ReorganizeHashOnlyOnSameNumber = 1510,

		/// <summary>
		/// ER_REORG_NO_PARAM_ERROR
		/// </summary>
		ReorganizeNoParameter = 1511,

		/// <summary>
		/// ER_ONLY_ON_RANGE_LIST_PARTITION
		/// </summary>
		OnlyOnRangeListPartition = 1512,

		/// <summary>
		/// ER_ADD_PARTITION_SUBPART_ERROR
		/// </summary>
		AddPartitionSubPartition = 1513,

		/// <summary>
		/// ER_ADD_PARTITION_NO_NEW_PARTITION
		/// </summary>
		AddPartitionNoNewPartition = 1514,

		/// <summary>
		/// ER_COALESCE_PARTITION_NO_PARTITION
		/// </summary>
		CoalescePartitionNoPartition = 1515,

		/// <summary>
		/// ER_REORG_PARTITION_NOT_EXIST
		/// </summary>
		ReorganizePartitionNotExist = 1516,

		/// <summary>
		/// ER_SAME_NAME_PARTITION
		/// </summary>
		SameNamePartition = 1517,

		/// <summary>
		/// ER_NO_BINLOG_ERROR
		/// </summary>
		NoBinLog = 1518,

		/// <summary>
		/// ER_CONSECUTIVE_REORG_PARTITIONS
		/// </summary>
		ConsecutiveReorganizePartitions = 1519,

		/// <summary>
		/// ER_REORG_OUTSIDE_RANGE
		/// </summary>
		ReorganizeOutsideRange = 1520,

		/// <summary>
		/// ER_PARTITION_FUNCTION_FAILURE
		/// </summary>
		PartitionFunctionFailure = 1521,

		/// <summary>
		/// ER_PART_STATE_ERROR
		/// </summary>
		PartitionStateError = 1522,

		/// <summary>
		/// ER_LIMITED_PART_RANGE
		/// </summary>
		LimitedPartitionRange = 1523,

		/// <summary>
		/// ER_PLUGIN_IS_NOT_LOADED
		/// </summary>
		PluginIsNotLoaded = 1524,

		/// <summary>
		/// ER_WRONG_VALUE
		/// </summary>
		WrongValue = 1525,

		/// <summary>
		/// ER_NO_PARTITION_FOR_GIVEN_VALUE
		/// </summary>
		NoPartitionForGivenValue = 1526,

		/// <summary>
		/// ER_FILEGROUP_OPTION_ONLY_ONCE
		/// </summary>
		FileGroupOptionOnlyOnce = 1527,

		/// <summary>
		/// ER_CREATE_FILEGROUP_FAILED
		/// </summary>
		CreateFileGroupFailed = 1528,

		/// <summary>
		/// ER_DROP_FILEGROUP_FAILED
		/// </summary>
		DropFileGroupFailed = 1529,

		/// <summary>
		/// ER_TABLESPACE_AUTO_EXTEND_ERROR
		/// </summary>
		TableSpaceAutoExtend = 1530,

		/// <summary>
		/// ER_WRONG_SIZE_NUMBER
		/// </summary>
		WrongSizeNumber = 1531,

		/// <summary>
		/// ER_SIZE_OVERFLOW_ERROR
		/// </summary>
		SizeOverflow = 1532,

		/// <summary>
		/// ER_ALTER_FILEGROUP_FAILED
		/// </summary>
		AlterFileGroupFailed = 1533,

		/// <summary>
		/// ER_BINLOG_ROW_LOGGING_FAILED
		/// </summary>
		BinLogRowLogginFailed = 1534,

		/// <summary>
		/// ER_BINLOG_ROW_WRONG_TABLE_DEF
		/// </summary>
		BinLogRowWrongTableDefinition = 1535,

		/// <summary>
		/// ER_BINLOG_ROW_RBR_TO_SBR
		/// </summary>
		BinLogRowRBRToSBR = 1536,

		/// <summary>
		/// ER_EVENT_ALREADY_EXISTS
		/// </summary>
		EventAlreadyExists = 1537,

		/// <summary>
		/// ER_EVENT_STORE_FAILED
		/// </summary>
		EventStoreFailed = 1538,

		/// <summary>
		/// ER_EVENT_DOES_NOT_EXIST
		/// </summary>
		EventDoesNotExist = 1539,

		/// <summary>
		/// ER_EVENT_CANT_ALTER
		/// </summary>
		EventCannotAlter = 1540,

		/// <summary>
		/// ER_EVENT_DROP_FAILED
		/// </summary>
		EventDropFailed = 1541,

		/// <summary>
		/// ER_EVENT_INTERVAL_NOT_POSITIVE_OR_TOO_BIG
		/// </summary>
		EventIntervalNotPositiveOrTooBig = 1542,

		/// <summary>
		/// ER_EVENT_ENDS_BEFORE_STARTS
		/// </summary>
		EventEndsBeforeStarts = 1543,

		/// <summary>
		/// ER_EVENT_EXEC_TIME_IN_THE_PAST
		/// </summary>
		EventExecTimeInThePast = 1544,

		/// <summary>
		/// ER_EVENT_OPEN_TABLE_FAILED
		/// </summary>
		EventOpenTableFailed = 1545,

		/// <summary>
		/// ER_EVENT_NEITHER_M_EXPR_NOR_M_AT
		/// </summary>
		EventNeitherMExpresssionNorMAt = 1546,

		/// <summary>
		/// ER_OBSOLETE_COL_COUNT_DOESNT_MATCH_CORRUPTED
		/// </summary>
		ColumnCountDoesNotMatchCorrupted = 1547,

		/// <summary>
		/// ER_OBSOLETE_CANNOT_LOAD_FROM_TABLE
		/// </summary>
		CannotLoadFromTable = 1548,

		/// <summary>
		/// ER_EVENT_CANNOT_DELETE
		/// </summary>
		EventCannotDelete = 1549,

		/// <summary>
		/// ER_EVENT_COMPILE_ERROR
		/// </summary>
		EventCompileError = 1550,

		/// <summary>
		/// ER_EVENT_SAME_NAME
		/// </summary>
		EventSameName = 1551,

		/// <summary>
		/// ER_EVENT_DATA_TOO_LONG
		/// </summary>
		EventDataTooLong = 1552,

		/// <summary>
		/// ER_DROP_INDEX_FK
		/// </summary>
		DropIndexForeignKey = 1553,

		/// <summary>
		/// ER_WARN_DEPRECATED_SYNTAX_WITH_VER
		/// </summary>
		WarningDeprecatedSyntaxWithVersion = 1554,

		/// <summary>
		/// ER_CANT_WRITE_LOCK_LOG_TABLE
		/// </summary>
		CannotWriteLockLogTable = 1555,

		/// <summary>
		/// ER_CANT_LOCK_LOG_TABLE
		/// </summary>
		CannotLockLogTable = 1556,

		/// <summary>
		/// ER_FOREIGN_DUPLICATE_KEY_OLD_UNUSED
		/// </summary>
		ForeignDuplicateKey = 1557,

		/// <summary>
		/// ER_COL_COUNT_DOESNT_MATCH_PLEASE_UPDATE
		/// </summary>
		ColumnCountDoesNotMatchPleaseUpdate = 1558,

		/// <summary>
		/// ER_TEMP_TABLE_PREVENTS_SWITCH_OUT_OF_RBR
		/// </summary>
		TemoraryTablePreventSwitchOutOfRBR = 1559,

		/// <summary>
		/// ER_STORED_FUNCTION_PREVENTS_SWITCH_BINLOG_FORMAT
		/// </summary>
		StoredFunctionPreventsSwitchBinLogFormat = 1560,

		/// <summary>
		/// ER_NDB_CANT_SWITCH_BINLOG_FORMAT
		/// </summary>
		NDBCannotSwitchBinLogFormat = 1561,

		/// <summary>
		/// ER_PARTITION_NO_TEMPORARY
		/// </summary>
		PartitionNoTemporary = 1562,

		/// <summary>
		/// ER_PARTITION_CONST_DOMAIN_ERROR
		/// </summary>
		PartitionConstantDomain = 1563,

		/// <summary>
		/// ER_PARTITION_FUNCTION_IS_NOT_ALLOWED
		/// </summary>
		PartitionFunctionIsNotAllowed = 1564,

		/// <summary>
		/// ER_DDL_LOG_ERROR
		/// </summary>
		DDLLogError = 1565,

		/// <summary>
		/// ER_NULL_IN_VALUES_LESS_THAN
		/// </summary>
		NullInValuesLessThan = 1566,

		/// <summary>
		/// ER_WRONG_PARTITION_NAME
		/// </summary>
		WrongPartitionName = 1567,

		/// <summary>
		/// ER_CANT_CHANGE_TX_CHARACTERISTICS
		/// </summary>
		CannotChangeTransactionIsolation = 1568,

		/// <summary>
		/// ER_DUP_ENTRY_AUTOINCREMENT_CASE
		/// </summary>
		DuplicateEntryAutoIncrementCase = 1569,

		/// <summary>
		/// ER_EVENT_MODIFY_QUEUE_ERROR
		/// </summary>
		EventModifyQueueError = 1570,

		/// <summary>
		/// ER_EVENT_SET_VAR_ERROR
		/// </summary>
		EventSetVariableError = 1571,

		/// <summary>
		/// ER_PARTITION_MERGE_ERROR
		/// </summary>
		PartitionMergeError = 1572,

		/// <summary>
		/// ER_CANT_ACTIVATE_LOG
		/// </summary>
		CannotActivateLog = 1573,

		/// <summary>
		/// ER_RBR_NOT_AVAILABLE
		/// </summary>
		RBRNotAvailable = 1574,

		/// <summary>
		/// ER_BASE64_DECODE_ERROR
		/// </summary>
		Base64DecodeError = 1575,

		/// <summary>
		/// ER_EVENT_RECURSION_FORBIDDEN
		/// </summary>
		EventRecursionForbidden = 1576,

		/// <summary>
		/// ER_EVENTS_DB_ERROR
		/// </summary>
		EventsDatabaseError = 1577,

		/// <summary>
		/// ER_ONLY_INTEGERS_ALLOWED
		/// </summary>
		OnlyIntegersAllowed = 1578,

		/// <summary>
		/// ER_UNSUPORTED_LOG_ENGINE
		/// </summary>
		UnsupportedLogEngine = 1579,

		/// <summary>
		/// ER_BAD_LOG_STATEMENT
		/// </summary>
		BadLogStatement = 1580,

		/// <summary>
		/// ER_CANT_RENAME_LOG_TABLE
		/// </summary>
		CannotRenameLogTable = 1581,

		/// <summary>
		/// ER_WRONG_PARAMCOUNT_TO_NATIVE_FCT
		/// </summary>
		WrongParameterCountToNativeFCT = 1582,

		/// <summary>
		/// ER_WRONG_PARAMETERS_TO_NATIVE_FCT
		/// </summary>
		WrongParametersToNativeFCT = 1583,

		/// <summary>
		/// ER_WRONG_PARAMETERS_TO_STORED_FCT
		/// </summary>
		WrongParametersToStoredFCT = 1584,

		/// <summary>
		/// ER_NATIVE_FCT_NAME_COLLISION
		/// </summary>
		NativeFCTNameCollision = 1585,

		/// <summary>
		/// ER_DUP_ENTRY_WITH_KEY_NAME
		/// </summary>
		DuplicateEntryWithKeyName = 1586,

		/// <summary>
		/// ER_BINLOG_PURGE_EMFILE
		/// </summary>
		BinLogPurgeEMFile = 1587,

		/// <summary>
		/// ER_EVENT_CANNOT_CREATE_IN_THE_PAST
		/// </summary>
		EventCannotCreateInThePast = 1588,

		/// <summary>
		/// ER_EVENT_CANNOT_ALTER_IN_THE_PAST
		/// </summary>
		EventCannotAlterInThePast = 1589,

		/// <summary>
		/// ER_SLAVE_INCIDENT
		/// </summary>
		SlaveIncident = 1590,

		/// <summary>
		/// ER_NO_PARTITION_FOR_GIVEN_VALUE_SILENT
		/// </summary>
		NoPartitionForGivenValueSilent = 1591,

		/// <summary>
		/// ER_BINLOG_UNSAFE_STATEMENT
		/// </summary>
		BinLogUnsafeStatement = 1592,

		/// <summary>
		/// ER_SLAVE_FATAL_ERROR
		/// </summary>
		SlaveFatalError = 1593,

		/// <summary>
		/// ER_SLAVE_RELAY_LOG_READ_FAILURE
		/// </summary>
		SlaveRelayLogReadFailure = 1594,

		/// <summary>
		/// ER_SLAVE_RELAY_LOG_WRITE_FAILURE
		/// </summary>
		SlaveRelayLogWriteFailure = 1595,

		/// <summary>
		/// ER_SLAVE_CREATE_EVENT_FAILURE
		/// </summary>
		SlaveCreateEventFailure = 1596,

		/// <summary>
		/// ER_SLAVE_MASTER_COM_FAILURE
		/// </summary>
		SlaveMasterComFailure = 1597,

		/// <summary>
		/// ER_BINLOG_LOGGING_IMPOSSIBLE
		/// </summary>
		BinLogLoggingImpossible = 1598,

		/// <summary>
		/// ER_VIEW_NO_CREATION_CTX
		/// </summary>
		ViewNoCreationContext = 1599,

		/// <summary>
		/// ER_VIEW_INVALID_CREATION_CTX
		/// </summary>
		ViewInvalidCreationContext = 1600,

		/// <summary>
		/// ER_SR_INVALID_CREATION_CTX
		/// </summary>
		StoredRoutineInvalidCreateionContext = 1601,

		/// <summary>
		/// ER_TRG_CORRUPTED_FILE
		/// </summary>
		TiggerCorruptedFile = 1602,

		/// <summary>
		/// ER_TRG_NO_CREATION_CTX
		/// </summary>
		TriggerNoCreationContext = 1603,

		/// <summary>
		/// ER_TRG_INVALID_CREATION_CTX
		/// </summary>
		TriggerInvalidCreationContext = 1604,

		/// <summary>
		/// ER_EVENT_INVALID_CREATION_CTX
		/// </summary>
		EventInvalidCreationContext = 1605,

		/// <summary>
		/// ER_TRG_CANT_OPEN_TABLE
		/// </summary>
		TriggerCannotOpenTable = 1606,

		/// <summary>
		/// ER_CANT_CREATE_SROUTINE
		/// </summary>
		CannoCreateSubRoutine = 1607,

		/// <summary>
		/// ER_NEVER_USED
		/// </summary>
		SlaveAmbiguousExecMode = 1608,

		/// <summary>
		/// ER_NO_FORMAT_DESCRIPTION_EVENT_BEFORE_BINLOG_STATEMENT
		/// </summary>
		NoFormatDescriptionEventBeforeBinLogStatement = 1609,

		/// <summary>
		/// ER_SLAVE_CORRUPT_EVENT
		/// </summary>
		SlaveCorruptEvent = 1610,

		/// <summary>
		/// ER_LOAD_DATA_INVALID_COLUMN
		/// </summary>
		LoadDataInvalidColumn = 1611,

		/// <summary>
		/// ER_LOG_PURGE_NO_FILE
		/// </summary>
		LogPurgeNoFile = 1612,

		/// <summary>
		/// ER_XA_RBTIMEOUT
		/// </summary>
		XARBTimeout = 1613,

		/// <summary>
		/// ER_XA_RBDEADLOCK
		/// </summary>
		XARBDeadlock = 1614,

		/// <summary>
		/// ER_NEED_REPREPARE
		/// </summary>
		NeedRePrepare = 1615,

		/// <summary>
		/// ER_DELAYED_NOT_SUPPORTED
		/// </summary>
		DelayedNotSupported = 1616,

		/// <summary>
		/// WARN_NO_MASTER_INFO
		/// </summary>
		WarningNoMasterInfo = 1617,

		/// <summary>
		/// WARN_OPTION_IGNORED
		/// </summary>
		WarningOptionIgnored = 1618,

		/// <summary>
		/// WARN_PLUGIN_DELETE_BUILTIN
		/// </summary>
		WarningPluginDeleteBuiltIn = 1619,

		/// <summary>
		/// WARN_PLUGIN_BUSY
		/// </summary>
		WarningPluginBusy = 1620,

		/// <summary>
		/// ER_VARIABLE_IS_READONLY
		/// </summary>
		VariableIsReadonly = 1621,

		/// <summary>
		/// ER_WARN_ENGINE_TRANSACTION_ROLLBACK
		/// </summary>
		WarningEngineTransactionRollback = 1622,

		/// <summary>
		/// ER_SLAVE_HEARTBEAT_FAILURE
		/// </summary>
		SlaveHeartbeatFailure = 1623,

		/// <summary>
		/// ER_SLAVE_HEARTBEAT_VALUE_OUT_OF_RANGE
		/// </summary>
		SlaveHeartbeatValueOutOfRange = 1624,

		/// <summary>
		/// ER_NDB_REPLICATION_SCHEMA_ERROR
		/// </summary>
		NDBReplicationSchemaError = 1625,

		/// <summary>
		/// ER_CONFLICT_FN_PARSE_ERROR
		/// </summary>
		ConflictFunctionParseError = 1626,

		/// <summary>
		/// ER_EXCEPTIONS_WRITE_ERROR
		/// </summary>
		ExcepionsWriteError = 1627,

		/// <summary>
		/// ER_TOO_LONG_TABLE_COMMENT
		/// </summary>
		TooLongTableComment = 1628,

		/// <summary>
		/// ER_TOO_LONG_FIELD_COMMENT
		/// </summary>
		TooLongFieldComment = 1629,

		/// <summary>
		/// ER_FUNC_INEXISTENT_NAME_COLLISION
		/// </summary>
		FunctionInExistentNameCollision = 1630,

		/// <summary>
		/// ER_DATABASE_NAME
		/// </summary>
		DatabaseNameError = 1631,

		/// <summary>
		/// ER_TABLE_NAME
		/// </summary>
		TableNameErrror = 1632,

		/// <summary>
		/// ER_PARTITION_NAME
		/// </summary>
		PartitionNameError = 1633,

		/// <summary>
		/// ER_SUBPARTITION_NAME
		/// </summary>
		SubPartitionNameError = 1634,

		/// <summary>
		/// ER_TEMPORARY_NAME
		/// </summary>
		TemporaryNameError = 1635,

		/// <summary>
		/// ER_RENAMED_NAME
		/// </summary>
		RenamedNameError = 1636,

		/// <summary>
		/// ER_TOO_MANY_CONCURRENT_TRXS
		/// </summary>
		TooManyConcurrentTransactions = 1637,

		/// <summary>
		/// WARN_NON_ASCII_SEPARATOR_NOT_IMPLEMENTED
		/// </summary>
		WarningNonASCIISeparatorNotImplemented = 1638,

		/// <summary>
		/// ER_DEBUG_SYNC_TIMEOUT
		/// </summary>
		DebugSyncTimeout = 1639,

		/// <summary>
		/// ER_DEBUG_SYNC_HIT_LIMIT
		/// </summary>
		DebugSyncHitLimit = 1640,
	}
}
