using System;
using System.Data;
using System.Data.Common;
using System.IO;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlParameter : DbParameter
	{
		public MySqlParameter()
		{
		}

		public override DbType DbType { get; set; }

		public override ParameterDirection Direction { get; set; }

		public override bool IsNullable { get; set; }

		public override string ParameterName
		{
			get
			{
				return m_name;
			}
			set
			{
				m_name = value;
				NormalizedParameterName = NormalizeParameterName(m_name);
			}
		}

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

#if !NETSTANDARD1_3
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

		internal MySqlParameter WithParameterName(string parameterName) => new MySqlParameter(this, parameterName);

		private MySqlParameter(MySqlParameter other, string parameterName)
		{
			DbType = other.DbType;
			Direction = other.Direction;
			IsNullable = other.IsNullable;
			Size = other.Size;
			ParameterName = parameterName ?? other.ParameterName;
			Value = other.Value;
#if NETSTANDARD1_3
			Precision = other.Precision;
			Scale = other.Scale;
#endif
		}

		internal string NormalizedParameterName { get; private set; }

		internal void AppendSqlString(BinaryWriter writer, StatementPreparerOptions options)
		{
			if (Value == null || Value == DBNull.Value)
			{
				writer.WriteUtf8("NULL");
			}
			else if (Value is string)
			{
				writer.Write((byte) '\'');
				writer.WriteUtf8(((string) Value).Replace("\\", "\\\\").Replace("'", "\\'"));
				writer.Write((byte) '\'');
			}
			else if (Value is byte || Value is sbyte || Value is short || Value is int || Value is long || Value is ushort || Value is uint || Value is ulong || Value is decimal)
			{
				writer.WriteUtf8("{0}".FormatInvariant(Value));
			}
			else if (Value is byte[])
			{
				// TODO: use a _binary'...' string for more efficient data transmission
				writer.WriteUtf8("_binary'");
				foreach (var by in (byte[]) Value)
				{
					if (by == 0x27 || by == 0x5C)
						writer.Write((byte) 0x5C);
					writer.Write(by);
				}
				writer.Write((byte) '\'');
			}
			else if (Value is bool)
			{
				writer.WriteUtf8(((bool) Value) ? "true" : "false");
			}
			else if (Value is float || Value is double)
			{
				writer.WriteUtf8("{0:R}".FormatInvariant(Value));
			}
			else if (Value is DateTime)
			{
				writer.WriteUtf8("timestamp '{0:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}'".FormatInvariant((DateTime) Value));
			}
			else if (Value is TimeSpan)
			{
				writer.WriteUtf8("time '");
				var ts = (TimeSpan) Value;
				if (ts.Ticks < 0)
				{
					writer.Write((byte) '-');
					ts = TimeSpan.FromTicks(-ts.Ticks);
				}
				writer.WriteUtf8("{0}:{1:mm':'ss'.'ffffff}'".FormatInvariant(ts.Days * 24 + ts.Hours, ts));
			}
			else if (Value is Guid)
			{
				if (options.HasFlag(StatementPreparerOptions.OldGuids))
				{
					writer.WriteUtf8("_binary'");
					writer.Write(((Guid) Value).ToByteArray());
					writer.Write((byte) '\'');
				}
				else
				{
					writer.WriteUtf8("'{0:D}'".FormatInvariant(Value));
				}
			}
			else if (DbType == DbType.Int16)
			{
				writer.WriteUtf8("{0}".FormatInvariant((short) Value));
			}
			else if (DbType == DbType.UInt16)
			{
				writer.WriteUtf8("{0}".FormatInvariant((ushort) Value));
			}
			else if (DbType == DbType.Int32)
			{
				writer.WriteUtf8("{0}".FormatInvariant((int) Value));
			}
			else if (DbType == DbType.UInt32)
			{
				writer.WriteUtf8("{0}".FormatInvariant((uint) Value));
			}
			else if (DbType == DbType.Int64)
			{
				writer.WriteUtf8("{0}".FormatInvariant((long) Value));
			}
			else if (DbType == DbType.UInt64)
			{
				writer.WriteUtf8("{0}".FormatInvariant((ulong) Value));
			}
			else
			{
				throw new NotSupportedException("Parameter type {0} (DbType: {1}) not currently supported. Value: {2}".FormatInvariant(Value.GetType().Name, DbType, Value));
			}
		}

		internal static string NormalizeParameterName(string name)
		{
			name = name.Trim();

			if ((name.StartsWith("@`", StringComparison.Ordinal) || name.StartsWith("?`", StringComparison.Ordinal)) && name.EndsWith("`", StringComparison.Ordinal))
				return name.Substring(2, name.Length - 3).Replace("``", "`");
			if ((name.StartsWith("@'", StringComparison.Ordinal) || name.StartsWith("?'", StringComparison.Ordinal)) && name.EndsWith("'", StringComparison.Ordinal))
				return name.Substring(2, name.Length - 3).Replace("''", "'");
			if ((name.StartsWith("@\"", StringComparison.Ordinal) || name.StartsWith("?\"", StringComparison.Ordinal)) && name.EndsWith("\"", StringComparison.Ordinal))
				return name.Substring(2, name.Length - 3).Replace("\"\"", "\"");

			return name.StartsWith("@", StringComparison.Ordinal) || name.StartsWith("?", StringComparison.Ordinal) ? name.Substring(1) : name;
		}

		string m_name;
	}
}
