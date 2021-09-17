global using System;
global using System.Data;
global using Dapper;
#if BASELINE
global using MySql.Data.MySqlClient;
#else
global using MySqlConnector;
#endif
global using Xunit;
