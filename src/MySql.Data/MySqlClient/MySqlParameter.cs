using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;

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
			else if (Value is byte || Value is sbyte || Value is short || Value is int || Value is long || Value is ushort || Value is uint || Value is ulong || Value is decimal)
			{
				output.AppendFormat(CultureInfo.InvariantCulture, "{0}", Value);
			}
			else if (Value is byte[])
			{
				// TODO: use a _binary'...' string for more efficient data transmission
				output.Append("X'");
				foreach (var by in (byte[]) Value)
					output.AppendFormat(CultureInfo.InvariantCulture, "{0:X2}", by);
				output.Append("'");
			}
			else if (Value is bool)
			{
				output.Append(((bool) Value) ? "true" : "false");
			}
			else if (Value is float || Value is double)
			{
				output.AppendFormat(CultureInfo.InvariantCulture, "{0:R}", Value);
			}
			else if (Value is DateTime)
			{
				output.AppendFormat(CultureInfo.InvariantCulture, "timestamp '{0:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}'", (DateTime) Value);
			}
			else if (Value is TimeSpan)
			{
				output.Append("time '");
				var ts = (TimeSpan) Value;
				if (ts.Ticks < 0)
				{
					output.Append('-');
					ts = TimeSpan.FromTicks(-ts.Ticks);
				}
				output.AppendFormat(CultureInfo.InvariantCulture, "{0}:{1:mm':'ss'.'ffffff}'", ts.Days * 24 + ts.Hours, ts);
			}
			else if (Value is Guid)
			{
				output.AppendFormat("'{0:D}'", Value);
			}
			else if (DbType == DbType.Int16)
			{
				output.AppendFormat(CultureInfo.InvariantCulture, "{0}", (short) Value);
			}
			else if (DbType == DbType.UInt16)
			{
				output.AppendFormat(CultureInfo.InvariantCulture, "{0}", (ushort) Value);
			}
			else if (DbType == DbType.Int32)
			{
				output.AppendFormat(CultureInfo.InvariantCulture, "{0}", (int) Value);
			}
			else if (DbType == DbType.UInt32)
			{
				output.AppendFormat(CultureInfo.InvariantCulture, "{0}", (uint) Value);
			}
			else if (DbType == DbType.Int64)
			{
				output.AppendFormat(CultureInfo.InvariantCulture, "{0}", (long) Value);
			}
			else if (DbType == DbType.UInt64)
			{
				output.AppendFormat(CultureInfo.InvariantCulture, "{0}", (ulong) Value);
			}
			else
			{
				throw new NotSupportedException("Parameter type {0} (DbType: {1}) not currently supported. Value: {2}".FormatInvariant(Value.GetType().Name, DbType, Value));
			}
		}
	}
}
