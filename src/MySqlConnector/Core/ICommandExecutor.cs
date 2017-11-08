using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core
{
	internal interface ICommandExecutor
	{
		Task<int> ExecuteNonQueryAsync(string commandText, MySqlParameterCollection parameterCollection, IOBehavior ioBehavior, CancellationToken cancellationToken);

		Task<object> ExecuteScalarAsync(string commandText, MySqlParameterCollection parameterCollection, IOBehavior ioBehavior, CancellationToken cancellationToken);

		Task<DbDataReader> ExecuteReaderAsync(string commandText, MySqlParameterCollection parameterCollection, CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken);
	}
}
