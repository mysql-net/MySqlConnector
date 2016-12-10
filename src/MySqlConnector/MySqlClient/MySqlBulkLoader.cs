using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Serialization;
using MySql.Data.Protocol.Serialization;

namespace MySql.Data.MySqlClient
{
    public class MySqlBulkLoader
    {
        private const string defaultFieldTerminator = "\t";
        private const string defaultLineTerminator = "\n";
        private const char defaultEscapeCharacter = '\\';

        private static Dictionary<string, Stream> InfileStreams = new Dictionary<string, Stream>();

        public string CharacterSet { get; set; }
        public List<string> Columns { get; }
        public MySqlBulkLoaderConflictOption ConflictOption { get; set; }
        public MySqlConnection Connection { get; set; }
        public char EscapeCharacter { get; set; }
        public List<string> Expressions { get; }
        public char FieldQuotationCharacter { get; set; }
        public bool FieldQuotationOptional { get; set; }
        public string FieldTerminator { get; set; }
        public string FileName { get; set; }
        public Stream InfileStream { get; set; }
        public string LinePrefix { get; set; }
        public string LineTerminator { get; set; }
        public bool Local { get; set; }
        public int NumberOfLinesToSkip { get; set; }
        public MySqlBulkLoaderPriority Priority { get; set; }
        public string TableName { get; set; }
        public int Timeout { get; set; }

        public MySqlBulkLoader(MySqlConnection connection)
        {
            this.Connection = connection;
            this.Local = true;
            this.FieldTerminator = "\t";
            this.LineTerminator = "\n";
            this.FieldQuotationCharacter = '\0';
            this.ConflictOption = MySqlBulkLoaderConflictOption.None;
            this.Columns = new List<string>();
            this.Expressions = new List<string>();
        }

        private string BuildSqlCommand()
        {
            if (string.IsNullOrWhiteSpace(this.FileName) || string.IsNullOrWhiteSpace(this.TableName))
            {
                //This is intentionally a different exception to what is thrown by the baseline client because
                //the baseline does not handle null or empty FileName and TableName.
                //The baseline client simply tries to use the given values, resulting in a NullReferenceException for
                //a null FileName, a MySqlException with an inner FileStream exception for an empty FileName,
                //and a MySqlException with a syntax error if the TableName is null or empty.
                throw new InvalidOperationException("FileName or InfileStream, and TableName are required.");
            }

            StringBuilder sqlCommandMain = new StringBuilder("LOAD DATA ");
            if (this.Priority == MySqlBulkLoaderPriority.Low)
            {
                sqlCommandMain.Append("LOW_PRIORITY ");
            }
            else if (this.Priority == MySqlBulkLoaderPriority.Concurrent)
            {
                sqlCommandMain.Append("CONCURRENT ");
            }
            if (this.Local)
            {
                sqlCommandMain.Append("LOCAL ");
            }
            sqlCommandMain.Append("INFILE ");
            if (System.IO.Path.DirectorySeparatorChar != '\\')
            {
                sqlCommandMain.AppendFormat("'{0}' ", this.FileName);
            }
            else
            {
                sqlCommandMain.AppendFormat("'{0}' ", this.FileName.Replace("\\", "\\\\"));
            }
            if (this.ConflictOption == MySqlBulkLoaderConflictOption.Ignore)
            {
                sqlCommandMain.Append("IGNORE ");
            }
            else if (this.ConflictOption == MySqlBulkLoaderConflictOption.Replace)
            {
                sqlCommandMain.Append("REPLACE ");
            }
            sqlCommandMain.AppendFormat("INTO TABLE {0} ", this.TableName);
            if (this.CharacterSet != null)
            {
                sqlCommandMain.AppendFormat("CHARACTER SET {0} ", this.CharacterSet);
            }

            StringBuilder sqlCommandFragment = new StringBuilder(string.Empty);
            if (this.FieldTerminator != "\t")
            {
                sqlCommandFragment.AppendFormat("TERMINATED BY '{0}' ", this.FieldTerminator);
            }
            if (this.FieldQuotationCharacter != 0)
            {
                sqlCommandFragment.AppendFormat("{0} ENCLOSED BY '{1}' ", (this.FieldQuotationOptional ? "OPTIONALLY" : ""), this.FieldQuotationCharacter);
            }
            if (this.EscapeCharacter != '\\' && this.EscapeCharacter != 0)
            {
                sqlCommandFragment.AppendFormat("ESCAPED BY '{0}' ", this.EscapeCharacter);
            }
            if (sqlCommandFragment.Length > 0)
            {
                sqlCommandMain.AppendFormat("FIELDS {0}", sqlCommandFragment.ToString());
            }

            sqlCommandFragment.Clear();
            if (this.LinePrefix != null && this.LinePrefix.Length > 0)
            {
                sqlCommandFragment.AppendFormat("STARTING BY '{0}' ", this.LinePrefix);
            }
            if (this.LineTerminator != "\n")
            {
                sqlCommandFragment.AppendFormat("TERMINATED BY '{0}' ", this.LineTerminator);
            }
            if (sqlCommandFragment.Length > 0)
            {
                sqlCommandMain.AppendFormat("LINES {0}", sqlCommandFragment.ToString());
            }

            if (this.NumberOfLinesToSkip > 0)
            {
                sqlCommandMain.AppendFormat("IGNORE {0} LINES ", this.NumberOfLinesToSkip);
            }
            if (this.Columns.Count > 0)
            {
                sqlCommandMain.Append("(");
                sqlCommandMain.Append(this.Columns[0]);
                for (int i = 1; i < this.Columns.Count; i++)
                {
                    sqlCommandMain.AppendFormat(",{0}", this.Columns[i]);
                }
                sqlCommandMain.Append(") ");
            }
            if (this.Expressions.Count > 0)
            {
                sqlCommandMain.Append("SET ");
                sqlCommandMain.Append(this.Expressions[0]);
                for (int j = 1; j < this.Expressions.Count; j++)
                {
                    sqlCommandMain.AppendFormat(",{0}", this.Expressions[j]);
                }
            }
            return sqlCommandMain.ToString();
        }

        public int Load()
        {
            return LoadAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<int> LoadAsync()
        {
            return await LoadAsync(IOBehavior.Asynchronous, CancellationToken.None);
        }

        public async Task<int> LoadAsync(CancellationToken cancellationToken)
        {
            return await LoadAsync(IOBehavior.Asynchronous, cancellationToken);
        }

        internal async Task<int> LoadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
        {
            int recordsAffected;
            bool closeConnection = false;
            if (this.Connection == null)
            {
                throw new InvalidOperationException("Connection not set");
            }
            if (this.Connection.State != ConnectionState.Open)
            {
                closeConnection = true;
                this.Connection.Open();
            }
            if (string.IsNullOrWhiteSpace(this.FileName) && this.InfileStream != null)
            {
                if (!this.Local)
                {
                    throw new InvalidOperationException("Cannot use InfileStream when Local is not true.");
                }
                string streamKey = string.Format("{0}:{1}", LocalInfilePayload.InfileStreamPrefix, Guid.NewGuid());
                InfileStreams.Add(streamKey, this.InfileStream);
                this.FileName = streamKey;
            }
            try
            {
                string commandString = this.BuildSqlCommand();
                var cmd = new MySqlCommand(commandString, this.Connection)
                {
                    CommandTimeout = this.Timeout
                };
                recordsAffected = await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken);
            }
            finally
            {
                if (closeConnection)
                {
                    this.Connection.Close();
                }
            }
            return recordsAffected;
        }

        internal static Stream GetInfileStreamByKey(string streamKey)
        {
            return InfileStreams[streamKey];
        }
    }
}
