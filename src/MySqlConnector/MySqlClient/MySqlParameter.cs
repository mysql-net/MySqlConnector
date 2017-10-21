using System;
using System.Data;
using System.Data.Common;
using System.IO;
using MySql.Data.MySqlClient.Types;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlParameter : DbParameter
	{
		public MySqlParameter()
		{
			m_mySqlDbType = MySqlDbType.VarChar;
		}

		public MySqlParameter(string name, object objValue)
		{
			m_mySqlDbType = MySqlDbType.VarChar;
			ParameterName = name;
			Value = objValue;
		}

		public override DbType DbType
		{
			get => m_dbType;
			set
			{
				m_dbType = value;
				m_mySqlDbType = TypeMapper.Mapper.GetMySqlDbTypeForDbType(value);
				HasSetDbType = true;
			}
		}

		public MySqlDbType MySqlDbType
		{
			get => m_mySqlDbType;
			set
			{
				m_dbType = TypeMapper.Mapper.GetDbTypeForMySqlDbType(value);
				m_mySqlDbType = value;
				HasSetDbType = true;
			}
		}

		public override ParameterDirection Direction
		{
			get => m_direction.GetValueOrDefault(ParameterDirection.Input);
			set
			{
				if (value != ParameterDirection.Input && value != ParameterDirection.Output &&
					value != ParameterDirection.InputOutput && value != ParameterDirection.ReturnValue)
				{
					throw new ArgumentOutOfRangeException(nameof(value), "{0} is not a supported value for ParameterDirection".FormatInvariant(value));
				}
				m_direction = value;
			}
		}

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
			m_dbType = other.m_dbType;
			m_mySqlDbType = other.m_mySqlDbType;
			m_direction = other.m_direction;
			HasSetDbType = other.HasSetDbType;
			IsNullable = other.IsNullable;
			Size = other.Size;
			ParameterName = parameterName ?? other.ParameterName;
			Value = other.Value;
#if NETSTANDARD1_3
			Precision = other.Precision;
			Scale = other.Scale;
#endif
		}

		internal bool HasSetDirection => m_direction.HasValue;

		internal bool HasSetDbType { get; set; }

		internal string NormalizedParameterName { get; private set; }

		internal void AppendSqlString(BinaryWriter writer, StatementPreparerOptions options)
		{
			if (Value == null || Value == DBNull.Value)
			{
				writer.WriteUtf8("NULL");
			}
			else if (Value is string stringValue)
			{
				writer.Write((byte) '\'');
				writer.WriteUtf8(stringValue.Replace("\\", "\\\\").Replace("'", "\\'"));
				writer.Write((byte) '\'');
			}
			else if (Value is byte || Value is sbyte || Value is short || Value is int || Value is long || Value is ushort || Value is uint || Value is ulong || Value is decimal)
			{
				writer.WriteUtf8("{0}".FormatInvariant(Value));
			}
			else if (Value is byte[] byteArrayValue)
			{
				// determine the number of bytes to be written
				const string c_prefix = "_binary'";
				var length = byteArrayValue.Length + c_prefix.Length + 1;
				foreach (var by in byteArrayValue)
				{
					if (by == 0x27 || by == 0x5C)
						length++;
				}

				((MemoryStream) writer.BaseStream).Capacity = (int) writer.BaseStream.Length + length;

				writer.WriteUtf8(c_prefix);
				foreach (var by in byteArrayValue)
				{
					if (by == 0x27 || by == 0x5C)
						writer.Write((byte) 0x5C);
					writer.Write(by);
				}
				writer.Write((byte) '\'');
			}
			else if (Value is bool boolValue)
			{
				writer.WriteUtf8(boolValue ? "true" : "false");
			}
			else if (Value is float || Value is double)
			{
				writer.WriteUtf8("{0:R}".FormatInvariant(Value));
			}
			else if (Value is DateTime)
			{
				writer.WriteUtf8("timestamp '{0:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}'".FormatInvariant(Value));
			}
			else if (Value is DateTimeOffset dateTimeOffsetValue)
			{
				// store as UTC as it will be read as such when deserialized from a timespan column
				writer.WriteUtf8("timestamp '{0:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}'".FormatInvariant(dateTimeOffsetValue.UtcDateTime));
			}
			else if (Value is TimeSpan ts)
			{
				writer.WriteUtf8("time '");
				if (ts.Ticks < 0)
				{
					writer.Write((byte) '-');
					ts = TimeSpan.FromTicks(-ts.Ticks);
				}
				writer.WriteUtf8("{0}:{1:mm':'ss'.'ffffff}'".FormatInvariant(ts.Days * 24 + ts.Hours, ts));
			}
			else if (Value is Guid guidValue)
			{
				if ((options & StatementPreparerOptions.OldGuids) != 0)
				{
					writer.WriteUtf8("_binary'");
					foreach (var by in guidValue.ToByteArray())
					{
						if (by == 0x27 || by == 0x5C)
							writer.Write((byte) 0x5C);
						writer.Write(by);
					}
					writer.Write((byte) '\'');
				}
				else
				{
					writer.WriteUtf8("'{0:D}'".FormatInvariant(guidValue));
				}
			}
			else if (MySqlDbType == MySqlDbType.Int16)
			{
				writer.WriteUtf8("{0}".FormatInvariant((short) Value));
			}
			else if (MySqlDbType == MySqlDbType.UInt16)
			{
				writer.WriteUtf8("{0}".FormatInvariant((ushort) Value));
			}
			else if (MySqlDbType == MySqlDbType.Int32)
			{
				writer.WriteUtf8("{0}".FormatInvariant((int) Value));
			}
			else if (MySqlDbType == MySqlDbType.UInt32)
			{
				writer.WriteUtf8("{0}".FormatInvariant((uint) Value));
			}
			else if (MySqlDbType == MySqlDbType.Int64)
			{
				writer.WriteUtf8("{0}".FormatInvariant((long) Value));
			}
			else if (MySqlDbType == MySqlDbType.UInt64)
			{
				writer.WriteUtf8("{0}".FormatInvariant((ulong) Value));
			}
			else if (Value is Enum)
			{
				writer.WriteUtf8("{0:d}".FormatInvariant(Value));
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

		DbType m_dbType;
		MySqlDbType m_mySqlDbType;
		string m_name;
		ParameterDirection? m_direction;
	}
}
