using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
    public class MySqlBulkLoader
    {
        private const string defaultFieldTerminator = "\t";

        private const string defaultLineTerminator = "\n";

        private const char defaultEscapeCharacter = '\\';

        private string fieldTerminator;

        private string lineTerminator;

        private string charSet;

        private string tableName;

        private int numLinesToIgnore;

        private MySqlConnection connection;

        private string filename;

        private int timeout;

        private bool local;

        private string linePrefix;

        private char fieldQuotationCharacter;

        private bool fieldQuotationOptional;

        private char escapeChar;

        private MySqlBulkLoaderPriority priority;

        private MySqlBulkLoaderConflictOption conflictOption;

        private List<string> columns;

        private List<string> expressions;

        public string CharacterSet
        {
            get
            {
                return this.charSet;
            }
            set
            {
                this.charSet = value;
            }
        }

        public List<string> Columns
        {
            get
            {
                return this.columns;
            }
        }

        public MySqlBulkLoaderConflictOption ConflictOption
        {
            get
            {
                return this.conflictOption;
            }
            set
            {
                this.conflictOption = value;
            }
        }

        public MySqlConnection Connection
        {
            get
            {
                return this.connection;
            }
            set
            {
                this.connection = value;
            }
        }

        public char EscapeCharacter
        {
            get
            {
                return this.escapeChar;
            }
            set
            {
                this.escapeChar = value;
            }
        }

        public List<string> Expressions
        {
            get
            {
                return this.expressions;
            }
        }

        public char FieldQuotationCharacter
        {
            get
            {
                return this.fieldQuotationCharacter;
            }
            set
            {
                this.fieldQuotationCharacter = value;
            }
        }

        public bool FieldQuotationOptional
        {
            get
            {
                return this.fieldQuotationOptional;
            }
            set
            {
                this.fieldQuotationOptional = value;
            }
        }

        public string FieldTerminator
        {
            get
            {
                return this.fieldTerminator;
            }
            set
            {
                this.fieldTerminator = value;
            }
        }

        public string FileName
        {
            get
            {
                return this.filename;
            }
            set
            {
                this.filename = value;
            }
        }

        public string LinePrefix
        {
            get
            {
                return this.linePrefix;
            }
            set
            {
                this.linePrefix = value;
            }
        }

        public string LineTerminator
        {
            get
            {
                return this.lineTerminator;
            }
            set
            {
                this.lineTerminator = value;
            }
        }

        public bool Local
        {
            get
            {
                return this.local;
            }
            set
            {
                this.local = value;
            }
        }

        public int NumberOfLinesToSkip
        {
            get
            {
                return this.numLinesToIgnore;
            }
            set
            {
                this.numLinesToIgnore = value;
            }
        }

        public MySqlBulkLoaderPriority Priority
        {
            get
            {
                return this.priority;
            }
            set
            {
                this.priority = value;
            }
        }

        public string TableName
        {
            get
            {
                return this.tableName;
            }
            set
            {
                this.tableName = value;
            }
        }

        public int Timeout
        {
            get
            {
                return this.timeout;
            }
            set
            {
                this.timeout = value;
            }
        }

        public MySqlBulkLoader(MySqlConnection connection)
        {
            this.Connection = connection;
            this.Local = true;
            this.FieldTerminator = "\t";
            this.LineTerminator = "\n";
            this.FieldQuotationCharacter = '\0';
            this.ConflictOption = MySqlBulkLoaderConflictOption.None;
            this.columns = new List<string>();
            this.expressions = new List<string>();
        }

        private string BuildSqlCommand()
        {
            StringBuilder stringBuilder = new StringBuilder("LOAD DATA ");
            if (this.Priority == MySqlBulkLoaderPriority.Low)
            {
                stringBuilder.Append("LOW_PRIORITY ");
            }
            else if (this.Priority == MySqlBulkLoaderPriority.Concurrent)
            {
                stringBuilder.Append("CONCURRENT ");
            }
            if (this.Local)
            {
                stringBuilder.Append("LOCAL ");
            }
            stringBuilder.Append("INFILE ");
            if (System.IO.Path.DirectorySeparatorChar != '\\')
            {
                stringBuilder.AppendFormat("'{0}' ", this.FileName);
            }
            else
            {
                stringBuilder.AppendFormat("'{0}' ", this.FileName.Replace("\\", "\\\\"));
            }
            if (this.ConflictOption == MySqlBulkLoaderConflictOption.Ignore)
            {
                stringBuilder.Append("IGNORE ");
            }
            else if (this.ConflictOption == MySqlBulkLoaderConflictOption.Replace)
            {
                stringBuilder.Append("REPLACE ");
            }
            stringBuilder.AppendFormat("INTO TABLE {0} ", this.TableName);
            if (this.CharacterSet != null)
            {
                stringBuilder.AppendFormat("CHARACTER SET {0} ", this.CharacterSet);
            }
            StringBuilder stringBuilder1 = new StringBuilder(string.Empty);
            if (this.FieldTerminator != "\t")
            {
                stringBuilder1.AppendFormat("TERMINATED BY '{0}' ", this.FieldTerminator);
            }
            if (this.FieldQuotationCharacter != 0)
            {
                stringBuilder1.AppendFormat("{0} ENCLOSED BY '{1}' ", (this.FieldQuotationOptional ? "OPTIONALLY" : ""), this.FieldQuotationCharacter);
            }
            if (this.EscapeCharacter != '\\' && this.EscapeCharacter != 0)
            {
                stringBuilder1.AppendFormat("ESCAPED BY '{0}' ", this.EscapeCharacter);
            }
            if (stringBuilder1.Length > 0)
            {
                stringBuilder.AppendFormat("FIELDS {0}", stringBuilder1.ToString());
            }
            stringBuilder1 = new StringBuilder(string.Empty);
            if (this.LinePrefix != null && this.LinePrefix.Length > 0)
            {
                stringBuilder1.AppendFormat("STARTING BY '{0}' ", this.LinePrefix);
            }
            if (this.LineTerminator != "\n")
            {
                stringBuilder1.AppendFormat("TERMINATED BY '{0}' ", this.LineTerminator);
            }
            if (stringBuilder1.Length > 0)
            {
                stringBuilder.AppendFormat("LINES {0}", stringBuilder1.ToString());
            }
            if (this.NumberOfLinesToSkip > 0)
            {
                stringBuilder.AppendFormat("IGNORE {0} LINES ", this.NumberOfLinesToSkip);
            }
            if (this.Columns.Count > 0)
            {
                stringBuilder.Append("(");
                stringBuilder.Append(this.Columns[0]);
                for (int i = 1; i < this.Columns.Count; i++)
                {
                    stringBuilder.AppendFormat(",{0}", this.Columns[i]);
                }
                stringBuilder.Append(") ");
            }
            if (this.Expressions.Count > 0)
            {
                stringBuilder.Append("SET ");
                stringBuilder.Append(this.Expressions[0]);
                for (int j = 1; j < this.Expressions.Count; j++)
                {
                    stringBuilder.AppendFormat(",{0}", this.Expressions[j]);
                }
            }
            return stringBuilder.ToString();
        }

        public int Load()
        {
            int num;
            bool flag = false;
            if (this.Connection == null)
            {
                //throw new InvalidOperationException(Resources.ConnectionNotSet);
                throw new InvalidOperationException("Connection not set");
            }
            if (this.connection.State != ConnectionState.Open)
            {
                flag = true;
                this.connection.Open();
            }
            try
            {
                string commandString = this.BuildSqlCommand();
                num = (new MySqlCommand(commandString, this.Connection)
                {
                    CommandTimeout = this.Timeout
                }).ExecuteNonQuery();
            }
            finally
            {
                if (flag)
                {
                    this.connection.Close();
                }
            }
            return num;
        }

        public Task<int> LoadAsync()
        {
            return this.LoadAsync(CancellationToken.None);
        }

        public Task<int> LoadAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
            if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    taskCompletionSource.SetResult(this.Load());
                }
                catch (Exception exception)
                {
                    taskCompletionSource.SetException(exception);
                }
            }
            else
            {
                taskCompletionSource.SetCanceled();
            }
            return taskCompletionSource.Task;
        }
    }
}
