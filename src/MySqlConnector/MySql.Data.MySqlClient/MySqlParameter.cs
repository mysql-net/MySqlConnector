using System;
using System.Data;
using System.Data.Common;
using System.IO;
using MySqlConnector.Core;
using MySqlConnector.Utilities;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlParameter : DbParameter, IDbDataParameter
	{
		public MySqlParameter()
		{
			ParameterName = "";
			SourceColumn = "";
#if !NETSTANDARD1_3
			SourceVersion = DataRowVersion.Current;
#endif
			ResetDbType();
		}

		public MySqlParameter(string name, object objValue)
			: this()
		{
			ParameterName = name;
			Value = objValue;
		}

		public MySqlParameter(string name, MySqlDbType mySqlDbType)
			: this(name, mySqlDbType, 0)
		{
		}

		public MySqlParameter(string name, MySqlDbType mySqlDbType, int size)
			: this(name, mySqlDbType, size, "")
		{
		}

		public MySqlParameter(string name, MySqlDbType mySqlDbType, int size, string sourceColumn)
		{
			ParameterName = name;
			MySqlDbType = mySqlDbType;
			Size = size;
			SourceColumn = sourceColumn;
#if !NETSTANDARD1_3
			SourceVersion = DataRowVersion.Current;
#endif
		}

#if !NETSTANDARD1_3
		public MySqlParameter(string name, MySqlDbType mySqlDbType, int size, ParameterDirection direction, bool isNullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value)
			: this(name, mySqlDbType, size, sourceColumn)
		{
			Direction = direction;
			IsNullable = isNullable;
			Precision = precision;
			Scale = scale;
			SourceVersion = sourceVersion;
			Value = value;
		}
#endif

		public override DbType DbType
		{
			get => m_dbType;
			set
			{
				m_dbType = value;
				m_mySqlDbType = TypeMapper.Instance.GetMySqlDbTypeForDbType(value);
				HasSetDbType = true;
			}
		}

		public MySqlDbType MySqlDbType
		{
			get => m_mySqlDbType;
			set
			{
				m_dbType = TypeMapper.Instance.GetDbTypeForMySqlDbType(value);
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

#if NET45
		public byte Precision { get; set; }
		public byte Scale { get; set; }
#else
		public override byte Precision { get; set; }
		public override byte Scale { get; set; }
#endif

		public override string ParameterName
		{
			get => m_name;
			set
			{
				m_name = value;
				NormalizedParameterName = value == null ? null : NormalizeParameterName(m_name);
			}
		}

		public override int Size { get; set; }

		public override string SourceColumn { get; set; }

		public override bool SourceColumnNullMapping { get; set; }

#if !NETSTANDARD1_3
		public override DataRowVersion SourceVersion { get; set; }
#endif

		public override object Value
		{
			get => m_value;
			set
			{
				m_value = value;
				if (!HasSetDbType && value != null)
				{
					var typeMapping = TypeMapper.Instance.GetDbTypeMapping(value.GetType());
					if (typeMapping != null)
					{
						m_dbType = typeMapping.DbTypes[0];
						m_mySqlDbType = TypeMapper.Instance.GetMySqlDbTypeForDbType(m_dbType);
					}
				}
			}
		}

		public override void ResetDbType()
		{
			m_mySqlDbType = MySqlDbType.VarChar;
			m_dbType = DbType.String;
			HasSetDbType = false;
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
#if !NET45
			Precision = other.Precision;
			Scale = other.Scale;
#endif
		}

		internal bool HasSetDirection => m_direction.HasValue;

		internal bool HasSetDbType { get; set; }

		internal string NormalizedParameterName { get; private set; }

		internal void AppendSqlString(BinaryWriter writer, StatementPreparerOptions options, string parameterName)
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
			else if (Value is char charValue)
			{
				writer.Write((byte) '\'');
				switch (charValue)
				{
				case '\'':
				case '\\':
					writer.Write((byte) '\\');
					writer.Write((byte) charValue);
					break;

				default:
					writer.WriteUtf8(charValue.ToString());
					break;
				}
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
			else if (Value is DateTime dateTimeValue)
			{
				if ((options & StatementPreparerOptions.DateTimeUtc) != 0 && dateTimeValue.Kind == DateTimeKind.Local)
					throw new MySqlException("DateTime.Kind must not be Local when DateTimeKind setting is Utc (parameter name: {0})".FormatInvariant(parameterName));
				else if ((options & StatementPreparerOptions.DateTimeLocal) != 0 && dateTimeValue.Kind == DateTimeKind.Utc)
					throw new MySqlException("DateTime.Kind must not be Utc when DateTimeKind setting is Local (parameter name: {0})".FormatInvariant(parameterName));

				writer.WriteUtf8("timestamp('{0:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')".FormatInvariant(dateTimeValue));
			}
			else if (Value is DateTimeOffset dateTimeOffsetValue)
			{
				// store as UTC as it will be read as such when deserialized from a timespan column
				writer.WriteUtf8("timestamp('{0:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')".FormatInvariant(dateTimeOffsetValue.UtcDateTime));
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
				StatementPreparerOptions guidOptions = options & StatementPreparerOptions.GuidFormatMask;
				if (guidOptions == StatementPreparerOptions.GuidFormatBinary16 ||
					guidOptions == StatementPreparerOptions.GuidFormatTimeSwapBinary16 ||
					guidOptions == StatementPreparerOptions.GuidFormatLittleEndianBinary16)
				{
					var bytes = guidValue.ToByteArray();
					if (guidOptions != StatementPreparerOptions.GuidFormatLittleEndianBinary16)
					{
						Utility.SwapBytes(bytes, 0, 3);
						Utility.SwapBytes(bytes, 1, 2);
						Utility.SwapBytes(bytes, 4, 5);
						Utility.SwapBytes(bytes, 6, 7);

						if (guidOptions == StatementPreparerOptions.GuidFormatTimeSwapBinary16)
						{
							Utility.SwapBytes(bytes, 0, 4);
							Utility.SwapBytes(bytes, 1, 5);
							Utility.SwapBytes(bytes, 2, 6);
							Utility.SwapBytes(bytes, 3, 7);
							Utility.SwapBytes(bytes, 0, 2);
							Utility.SwapBytes(bytes, 1, 3);
						}
					}
					writer.WriteUtf8("_binary'");
					foreach (var by in bytes)
					{
						if (by == 0x27 || by == 0x5C)
							writer.Write((byte) 0x5C);
						writer.Write(by);
					}
					writer.Write((byte) '\'');
				}
				else if (guidOptions == StatementPreparerOptions.GuidFormatChar32)
				{
					writer.WriteUtf8("'{0:N}'".FormatInvariant(guidValue));
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
			else if ((MySqlDbType == MySqlDbType.String || MySqlDbType == MySqlDbType.VarChar) && HasSetDbType && Value is Enum)
			{
				writer.WriteUtf8("'{0:G}'".FormatInvariant(Value));
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
		object m_value;
	}
}
