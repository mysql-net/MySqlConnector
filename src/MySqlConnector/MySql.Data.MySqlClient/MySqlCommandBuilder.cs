#if !NETSTANDARD1_3
using System;
using System.Data;
using System.Data.Common;
using MySqlConnector.Utilities;

namespace MySql.Data.MySqlClient
{
	public class MySqlCommandBuilder : DbCommandBuilder
	{
		public MySqlCommandBuilder()
		{
			QuotePrefix = "`";
			QuoteSuffix = "`";
		}

		public MySqlCommandBuilder(MySqlDataAdapter dataAdapter)
			: this()
		{
			DataAdapter = dataAdapter;
		}

		public new MySqlDataAdapter DataAdapter
		{
			get => (MySqlDataAdapter) base.DataAdapter;
			set => base.DataAdapter = value;
		}

		public new MySqlCommand GetDeleteCommand() => (MySqlCommand) base.GetDeleteCommand();
		public new MySqlCommand GetInsertCommand() => (MySqlCommand) base.GetInsertCommand();
		public new MySqlCommand GetUpdateCommand() => (MySqlCommand) base.GetUpdateCommand();

		protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
		{
			((MySqlParameter) parameter).MySqlDbType = (MySqlDbType) row[SchemaTableColumn.ProviderType];
		}

		protected override string GetParameterName(int parameterOrdinal) => "@p{0}".FormatInvariant(parameterOrdinal);
		protected override string GetParameterName(string parameterName) => "@" + parameterName;
		protected override string GetParameterPlaceholder(int parameterOrdinal) => "@p{0}".FormatInvariant(parameterOrdinal);

		protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
		{
			if (!(adapter is MySqlDataAdapter mySqlDataAdapter))
				throw new ArgumentException("adapter needs to be a MySqlDataAdapter", nameof(adapter));

			if (adapter == DataAdapter)
				mySqlDataAdapter.RowUpdating -= RowUpdatingHandler;
			else
				mySqlDataAdapter.RowUpdating += RowUpdatingHandler;
		}

		public override string QuoteIdentifier(string unquotedIdentifier) => QuotePrefix + unquotedIdentifier.Replace("`", "``") + QuoteSuffix;

		public override string UnquoteIdentifier(string quotedIdentifier)
		{
			if (quotedIdentifier.Length >= 2 && quotedIdentifier[0] == QuotePrefix[0] && quotedIdentifier[quotedIdentifier.Length - 1] == QuoteSuffix[0])
				quotedIdentifier = quotedIdentifier.Substring(1, quotedIdentifier.Length - 2);
			return quotedIdentifier.Replace("``", "`");
		}

		private void RowUpdatingHandler(object sender, MySqlRowUpdatingEventArgs e) => RowUpdatingHandler(e);
	}
}
#endif
