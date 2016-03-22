// Copyright © 2013, 2015, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlConnectionStringBuilder : DbConnectionStringBuilder
	{
		internal Dictionary<string, object> values = new Dictionary<string, object>();
		//internal Dictionary<string, object> values
		//{
		//  get { lock (this) { return _values; } }
		//}

		private static MySqlConnectionStringOptionCollection options = new MySqlConnectionStringOptionCollection();

		static MySqlConnectionStringBuilder()
		{
			// Server options
			options.Add(new MySqlConnectionStringOption("server", "host,data source,datasource,address,addr,network address", typeof(string), "" /*"localhost"*/, false));
			options.Add(new MySqlConnectionStringOption("database", "initial catalog", typeof(string), string.Empty, false));
			options.Add(new MySqlConnectionStringOption("port", null, typeof(uint), (uint) 3306, false));
			options.Add(new MySqlConnectionStringOption("allowbatch", "allow batch", typeof(bool), true, false));
			options.Add(new MySqlConnectionStringOption("connectiontimeout", "connection timeout,connect timeout", typeof(uint), (uint) 15, false,
			  delegate (MySqlConnectionStringBuilder msb, MySqlConnectionStringOption sender, object Value)
			  {
				  uint value = (uint) Convert.ChangeType(Value, sender.BaseType);
				  // Timeout in milliseconds should not exceed maximum for 32 bit
				  // signed integer (~24 days). We truncate the value if it exceeds 
				  // maximum (MySqlCommand.CommandTimeout uses the same technique
				  uint timeout = Math.Min(value, Int32.MaxValue / 1000);
				  msb.SetValue("connectiontimeout", timeout);
			  },
			  delegate (MySqlConnectionStringBuilder msb, MySqlConnectionStringOption sender)
			  {
				  return (uint) msb.values["connectiontimeout"];
			  }
			  ));
			options.Add(new MySqlConnectionStringOption("defaultcommandtimeout", "command timeout,default command timeout", typeof(uint), (uint) 30, false));

			// authentication options
			options.Add(new MySqlConnectionStringOption("user id", "uid,username,user name,user,userid", typeof(string), "", false));
			options.Add(new MySqlConnectionStringOption("password", "pwd", typeof(string), "", false));
			options.Add(new MySqlConnectionStringOption("persistsecurityinfo", "persist security info", typeof(bool), false, false));

			// Other properties
			options.Add(new MySqlConnectionStringOption("treattinyasboolean", "treat tiny as boolean", typeof(bool), true, false));

			// language and charset options
			options.Add(new MySqlConnectionStringOption("characterset", "character set,charset", typeof(string), "", false));
		}

		public MySqlConnectionStringBuilder()
		{
			// Populate initial values
			lock (this)
			{
				for (int i = 0; i < options.Options.Count; i++)
				{
					values[options.Options[i].Keyword] = options.Options[i].DefaultValue;
				}
			}
		}

		public MySqlConnectionStringBuilder(string connStr)
		  : base()
		{
			lock (this)
			{
				ConnectionString = connStr;
			}
		}

		/// <summary>
		/// Gets or sets the name of the server.
		/// </summary>
		/// <value>The server.</value>
		public string Server
		{
			get { return this["server"] as string; }
			set { this["server"] = value; }
		}

		/// <summary>
		/// Gets or sets the name of the database the connection should 
		/// initially connect to.
		/// </summary>
		public string Database
		{
			get { return values["database"] as string; }
			set { SetValue("database", value); }
		}

		/// <summary>
		/// Gets or sets a boolean value that indicates whether this connection will allow
		/// commands to send multiple SQL statements in one execution.
		/// </summary>
		public bool AllowBatch
		{
			get { return (bool) values["allowbatch"]; }
			set { SetValue("allowbatch", value); }
		}

		/// <summary>
		/// Gets or sets the port number that is used when the socket
		/// protocol is being used.
		/// </summary>
		public uint Port
		{
			get { return (uint) values["port"]; }
			set { SetValue("port", value); }
		}

		/// <summary>
		/// Gets or sets the connection timeout.
		/// </summary>
		public uint ConnectionTimeout
		{
			get { return (uint) values["connectiontimeout"]; }

			set
			{
				// Timeout in milliseconds should not exceed maximum for 32 bit
				// signed integer (~24 days). We truncate the value if it exceeds 
				// maximum (MySqlCommand.CommandTimeout uses the same technique
				uint timeout = Math.Min(value, Int32.MaxValue / 1000);
				SetValue("connectiontimeout", timeout);
			}
		}

		/// <summary>
		/// Gets or sets the default command timeout.
		/// </summary>
		public uint DefaultCommandTimeout
		{
			get { return (uint) values["defaultcommandtimeout"]; }
			set { SetValue("defaultcommandtimeout", value); }
		}

		/// <summary>
		/// Gets or sets the user id that should be used to connect with.
		/// </summary>
		public string UserID
		{
			get { return (string) values["user id"]; }
			set { SetValue("user id", value); }
		}

		/// <summary>
		/// Gets or sets the password that should be used to connect with.
		/// </summary>
		public string Password
		{
			get { return (string) values["password"]; }
			set { SetValue("password", value); }
		}

		/// <summary>
		/// Gets or sets a boolean value that indicates if the password should be persisted
		/// in the connection string.
		/// </summary>
		public bool PersistSecurityInfo
		{
			get { return (bool) values["persistsecurityinfo"]; }
			set { SetValue("persistsecurityinfo", value); }
		}

		public bool TreatTinyAsBoolean
		{
			get { return (bool) values["treattinyasboolean"]; }
			set { SetValue("treattinyasboolean", value); }
		}

		/// <summary>
		/// Gets or sets the character set that should be used for sending queries to the server.
		/// </summary>
		public string CharacterSet
		{
			get { return (string) values["characterset"]; }
			set { SetValue("characterset", value); }
		}

		public override object this[string keyword]
		{
			get { MySqlConnectionStringOption opt = GetOption(keyword); return opt.Getter(this, opt); }
			set { MySqlConnectionStringOption opt = GetOption(keyword); opt.Setter(this, opt, value); }
		}

		public override void Clear()
		{
			base.Clear();
			lock (this)
			{
				foreach (var option in options.Options)
					if (option.DefaultValue != null)
						values[option.Keyword] = option.DefaultValue;
					else
						values[option.Keyword] = null;
			}
		}

		internal void SetValue(string keyword, object value)
		{
			MySqlConnectionStringOption option = GetOption(keyword);
			option.ValidateValue(ref value);

			// remove all related keywords
			option.Clean(this);

			if (value != null)
			{
				lock (this)
				{
					// set value for the given keyword
					values[option.Keyword] = value;
					base[keyword] = value;
				}
			}
		}

		private MySqlConnectionStringOption GetOption(string key)
		{
			MySqlConnectionStringOption option = options.Get(key);
			if (option == null)
				throw new ArgumentException($"Keyword not supported: {key}", nameof(key));
			else
				return option;
		}

		public override bool ContainsKey(string keyword)
		{
			MySqlConnectionStringOption option = options.Get(keyword);
			return option != null;
		}

		public override bool Remove(string keyword)
		{
			bool removed = false;
			lock (this) { removed = base.Remove(keyword); }
			if (!removed) return false;
			MySqlConnectionStringOption option = GetOption(keyword);
			lock (this)
			{
				values[option.Keyword] = option.DefaultValue;
			}
			return true;
		}

		public string GetConnectionString(bool includePass)
		{
			if (includePass) return ConnectionString;

			StringBuilder conn = new StringBuilder();
			string delimiter = "";
			foreach (string key in this.Keys)
			{
				if (String.Compare(key, "password", StringComparison.OrdinalIgnoreCase) == 0 ||
					String.Compare(key, "pwd", StringComparison.OrdinalIgnoreCase) == 0) continue;
				conn.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}={2}",
					delimiter, key, this[key]);
				delimiter = ";";
			}
			return conn.ToString();
		}

		public override bool Equals(object obj)
		{
			MySqlConnectionStringBuilder other = obj as MySqlConnectionStringBuilder;
			if (obj == null)
				return false;

			if (this.values.Count != other.values.Count) return false;

			foreach (KeyValuePair<string, object> kvp in this.values)
			{
				if (other.values.ContainsKey(kvp.Key))
				{
					object v = other.values[kvp.Key];
					if (v == null && kvp.Value != null) return false;
					if (kvp.Value == null && v != null) return false;
					if (kvp.Value == null && v == null) return true;
					if (!v.Equals(kvp.Value)) return false;
				}
				else
				{
					return false;
				}
			}

			return true;
		}

		public override int GetHashCode() => 0; // don't put these in a hash table
	}

	class MySqlConnectionStringOption
	{
		public MySqlConnectionStringOption(string keyword, string synonyms, Type baseType, object defaultValue, bool obsolete,
		  SetterDelegate setter, GetterDelegate getter)
		{
			Keyword = keyword;
			if (synonyms != null)
				Synonyms = synonyms.Split(',');
			BaseType = baseType;
			Obsolete = obsolete;
			DefaultValue = defaultValue;
			Setter = setter;
			Getter = getter;
		}

		public MySqlConnectionStringOption(string keyword, string synonyms, Type baseType, object defaultValue, bool obsolete) :
			this(keyword, synonyms, baseType, defaultValue, obsolete,
				delegate(MySqlConnectionStringBuilder msb, MySqlConnectionStringOption sender, object value)
				{
					sender.ValidateValue(ref value);
					msb.SetValue(sender.Keyword, Convert.ChangeType(value, sender.BaseType));
				},
				(msb, sender) => msb.values[sender.Keyword]
			)
		{
		}

		public string[] Synonyms { get; }
		public bool Obsolete { get; private set; }
		public Type BaseType { get; }
		public string Keyword { get; }
		public object DefaultValue { get; }
		public SetterDelegate Setter { get; }
		public GetterDelegate Getter { get; }

		public delegate void SetterDelegate(MySqlConnectionStringBuilder msb, MySqlConnectionStringOption sender, object value);
		public delegate object GetterDelegate(MySqlConnectionStringBuilder msb, MySqlConnectionStringOption sender);

		public bool HasKeyword(string key)
		{
			if (Keyword == key) return true;
			if (Synonyms == null) return false;
			foreach (var syn in Synonyms)
				if (syn == key) return true;
			return false;
		}

		public void Clean(MySqlConnectionStringBuilder builder)
		{
			builder.Remove(Keyword);
			if (Synonyms == null) return;
			foreach (var syn in Synonyms)
				builder.Remove(syn);
		}

		public void ValidateValue(ref object value)
		{
			bool b;
			if (value == null) return;
			string typeName = BaseType.Name;
			Type valueType = value.GetType();
			if (valueType.Name == "String")
			{
				if (BaseType == valueType) return;
				else if (BaseType == typeof(bool))
				{
					if (string.Compare("yes", (string) value, StringComparison.OrdinalIgnoreCase) == 0) value = true;
					else if (string.Compare("no", (string) value, StringComparison.OrdinalIgnoreCase) == 0) value = false;
					else if (Boolean.TryParse(value.ToString(), out b)) value = b;
					else throw new ArgumentException($"Value not correct for its type: {value}", nameof(value));
					return;
				}
			}

			if (typeName == "Boolean" && Boolean.TryParse(value.ToString(), out b)) { value = b; return; }

			UInt64 uintVal;
			if (typeName.StartsWith("UInt64") && UInt64.TryParse(value.ToString(), out uintVal)) { value = uintVal; return; }

			UInt32 uintVal32;
			if (typeName.StartsWith("UInt32") && UInt32.TryParse(value.ToString(), out uintVal32)) { value = uintVal32; return; }

			Int64 intVal;
			if (typeName.StartsWith("Int64") && Int64.TryParse(value.ToString(), out intVal)) { value = intVal; return; }

			Int32 intVal32;
			if (typeName.StartsWith("Int32") && Int32.TryParse(value.ToString(), out intVal32)) { value = intVal32; return; }

			throw new ArgumentException($"Value not correct for its type: {value}", nameof(value));
		}

		private bool ParseEnum(string requestedValue, out object value)
		{
			value = null;
			try
			{
				value = Enum.Parse(BaseType, requestedValue, true);
				return true;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

	}

	internal class MySqlConnectionStringOptionCollection : Dictionary<string, MySqlConnectionStringOption>
	{
		List<MySqlConnectionStringOption> options;

		internal List<MySqlConnectionStringOption> Options { get { return options; } }

		internal MySqlConnectionStringOptionCollection() : base(StringComparer.OrdinalIgnoreCase)
		{
			options = new List<MySqlConnectionStringOption>();
		}

		internal void Add(MySqlConnectionStringOption option)
		{
			options.Add(option);
			// Register the option with all the keywords.
			base.Add(option.Keyword, option);
			if (option.Synonyms != null)
			{
				for (int i = 0; i < option.Synonyms.Length; i++)
					base.Add(option.Synonyms[i], option);
			}
		}

		internal MySqlConnectionStringOption Get(string keyword)
		{
			MySqlConnectionStringOption option = null;
			base.TryGetValue(keyword, out option);
			return option;
		}
	}
}
