#if !NETSTANDARD1_3
using System;
using System.Data;
using System.Data.Common;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlDataAdapter : DbDataAdapter
	{
		public MySqlDataAdapter()
		{
			GC.SuppressFinalize(this);
		}

		public MySqlDataAdapter(MySqlCommand selectCommand)
			: this()
		{
			SelectCommand = selectCommand;
		}

		public MySqlDataAdapter(string selectCommandText, MySqlConnection connection)
			: this(new MySqlCommand(selectCommandText, connection))
		{
		}

		public MySqlDataAdapter(string selectCommandText, string connectionString)
			: this(new MySqlCommand(selectCommandText, new MySqlConnection(connectionString)))
		{
		}

		public event MySqlRowUpdatingEventHandler RowUpdating;

		public event MySqlRowUpdatedEventHandler RowUpdated;

		public new MySqlCommand DeleteCommand
		{
			get => (MySqlCommand) base.DeleteCommand;
			set => base.DeleteCommand = value;
		}

		public new MySqlCommand InsertCommand
		{
			get => (MySqlCommand) base.InsertCommand;
			set => base.InsertCommand = value;
		}

		public new MySqlCommand SelectCommand
		{
			get => (MySqlCommand) base.SelectCommand;
			set => base.SelectCommand = value;
		}

		public new MySqlCommand UpdateCommand
		{
			get => (MySqlCommand) base.UpdateCommand;
			set => base.UpdateCommand = value;
		}

		protected override void OnRowUpdating(RowUpdatingEventArgs value) => RowUpdating?.Invoke(this, (MySqlRowUpdatingEventArgs) value);

		protected override void OnRowUpdated(RowUpdatedEventArgs value) => RowUpdated?.Invoke(this, (MySqlRowUpdatedEventArgs) value);

		protected override RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping) => new MySqlRowUpdatingEventArgs(dataRow, command, statementType, tableMapping);

		protected override RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping) => new MySqlRowUpdatedEventArgs(dataRow, command, statementType, tableMapping);
	}

	public delegate void MySqlRowUpdatingEventHandler(object sender, MySqlRowUpdatingEventArgs e);

	public delegate void MySqlRowUpdatedEventHandler(object sender, MySqlRowUpdatedEventArgs e);

	public sealed class MySqlRowUpdatingEventArgs : RowUpdatingEventArgs
	{
		public MySqlRowUpdatingEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
			: base(row, command, statementType, tableMapping)
		{
		}

		public new MySqlCommand Command => (MySqlCommand) base.Command;
	}

	public sealed class MySqlRowUpdatedEventArgs : RowUpdatedEventArgs
	{
		public MySqlRowUpdatedEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
			: base(row, command, statementType, tableMapping)
		{
		}

		public new MySqlCommand Command => (MySqlCommand) base.Command;
	}
}
#endif
