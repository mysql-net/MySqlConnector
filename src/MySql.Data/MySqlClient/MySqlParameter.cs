using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using static System.FormattableString;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlParameter : DbParameter
	{
		public override DbType DbType { get; set; }

		public override ParameterDirection Direction { get; set; }

		public override bool IsNullable { get; set; }

		public override string ParameterName { get; set; }

		public override int Size { get; set; }

		public override string SourceColumn
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		public override bool SourceColumnNullMapping
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

#if !DNXCORE50
		public override DataRowVersion SourceVersion
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}
#endif

		public override object Value { get; set; }

		public override void ResetDbType()
		{
			DbType = default(DbType);
		}

		internal bool ParameterNameMatches(string name)
		{
			if (string.IsNullOrEmpty(ParameterName) || string.IsNullOrEmpty(name))
				return false;

			int thisNameIndex = 0, thisNameLength = ParameterName.Length;
			if (ParameterName[0] == '?' || ParameterName[0] == '@')
			{
				thisNameIndex++;
				thisNameLength--;
			}
			int otherNameIndex = 0, otherNameLength = name.Length;
			if (name[0] == '?' || name[0] == '@')
			{
				otherNameIndex++;
				otherNameLength--;
			}

			return thisNameLength == otherNameLength &&
				string.Compare(ParameterName, thisNameIndex, name, otherNameIndex, thisNameLength, StringComparison.OrdinalIgnoreCase) == 0;
		}

		internal void AppendSqlString(StringBuilder output)
		{
			if (Value == DBNull.Value)
			{
				output.Append("NULL");
			}
			else if (Value is string)
			{
				output.Append('\'');
				output.Append(((string) Value).Replace("\\", "\\\\").Replace("'", "\\'"));
				output.Append('\'');
			}
			else if (Value is short || Value is int || Value is long || Value is ushort || Value is uint || Value is ulong)
			{
				output.AppendFormat(CultureInfo.InvariantCulture, "{0}", Value);
			}
			else
			{
				throw new NotSupportedException(Invariant($"Parameter type {Value.GetType().Name} (DbType: {DbType}) not currently supported. Value: {Value}"));
			}
		}
	}
}
