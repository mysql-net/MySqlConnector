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
            Connection = connection;
            Local = true;
            FieldTerminator = defaultFieldTerminator;
            LineTerminator = defaultLineTerminator;
            FieldQuotationCharacter = '\0';
            ConflictOption = MySqlBulkLoaderConflictOption.None;
            Columns = new List<string>();
            Expressions = new List<string>();
        }

        private string BuildSqlCommand()
        {
            if (string.IsNullOrWhiteSpace(FileName) || string.IsNullOrWhiteSpace(TableName))
            {
                //This is intentionally a different exception to what is thrown by the baseline client because
                //the baseline does not handle null or empty FileName and TableName.
                //The baseline client simply tries to use the given values, resulting in a NullReferenceException for
                //a null FileName, a MySqlException with an inner FileStream exception for an empty FileName,
                //and a MySqlException with a syntax error if the TableName is null or empty.
                throw new InvalidOperationException("FileName or InfileStream, and TableName are required.");
            }

            StringBuilder sqlCommandMain = new StringBuilder("LOAD DATA ");
            if (Priority == MySqlBulkLoaderPriority.Low)
            {
                sqlCommandMain.Append("LOW_PRIORITY ");
            }
            else if (Priority == MySqlBulkLoaderPriority.Concurrent)
            {
                sqlCommandMain.Append("CONCURRENT ");
            }
            if (Local)
            {
                sqlCommandMain.Append("LOCAL ");
            }
            sqlCommandMain.Append("INFILE ");
            if (System.IO.Path.DirectorySeparatorChar != '\\')
            {
                sqlCommandMain.AppendFormat("'{0}' ", FileName);
            }
            else
            {
                sqlCommandMain.AppendFormat("'{0}' ", FileName.Replace("\\", "\\\\"));
            }
            if (ConflictOption == MySqlBulkLoaderConflictOption.Ignore)
            {
                sqlCommandMain.Append("IGNORE ");
            }
            else if (ConflictOption == MySqlBulkLoaderConflictOption.Replace)
            {
                sqlCommandMain.Append("REPLACE ");
            }
            sqlCommandMain.AppendFormat("INTO TABLE {0} ", TableName);
            if (CharacterSet != null)
            {
                sqlCommandMain.AppendFormat("CHARACTER SET {0} ", CharacterSet);
            }

            StringBuilder sqlCommandFragment = new StringBuilder(string.Empty);
            if (FieldTerminator != defaultFieldTerminator)
            {
                sqlCommandFragment.AppendFormat("TERMINATED BY \'{0}\' ", FieldTerminator);
            }
            if (FieldQuotationCharacter != 0)
            {
                sqlCommandFragment.AppendFormat("{0} ENCLOSED BY \'{1}\' ", (FieldQuotationOptional ? "OPTIONALLY" : ""), FieldQuotationCharacter);
            }
            if (EscapeCharacter != defaultEscapeCharacter && EscapeCharacter != 0)
            {
                sqlCommandFragment.AppendFormat("ESCAPED BY \'{0}\' ", EscapeCharacter);
            }
            if (sqlCommandFragment.Length > 0)
            {
                sqlCommandMain.AppendFormat("FIELDS {0}", sqlCommandFragment.ToString());
            }

            sqlCommandFragment.Clear();
            if (LinePrefix != null && LinePrefix.Length > 0)
            {
                sqlCommandFragment.AppendFormat("STARTING BY \'{0}\' ", LinePrefix);
            }
            if (LineTerminator != defaultLineTerminator)
            {
                sqlCommandFragment.AppendFormat("TERMINATED BY \'{0}\' ", LineTerminator);
            }
            if (sqlCommandFragment.Length > 0)
            {
                sqlCommandMain.AppendFormat("LINES {0}", sqlCommandFragment.ToString());
            }

            if (NumberOfLinesToSkip > 0)
            {
                sqlCommandMain.AppendFormat("IGNORE {0} LINES ", NumberOfLinesToSkip);
            }
            if (Columns.Count > 0)
            {
                sqlCommandMain.Append("(");
                sqlCommandMain.Append(Columns[0]);
                for (int i = 1; i < Columns.Count; i++)
                {
                    sqlCommandMain.AppendFormat(",{0}", Columns[i]);
                }
                sqlCommandMain.Append(") ");
            }
            if (Expressions.Count > 0)
            {
                sqlCommandMain.Append("SET ");
                sqlCommandMain.Append(Expressions[0]);
                for (int j = 1; j < Expressions.Count; j++)
                {
                    sqlCommandMain.AppendFormat(",{0}", Expressions[j]);
                }
            }
            return sqlCommandMain.ToString();
        }

        public int Load() => LoadAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

        public Task<int> LoadAsync() => LoadAsync(IOBehavior.Asynchronous, CancellationToken.None);

        public Task<int> LoadAsync(CancellationToken cancellationToken) => LoadAsync(IOBehavior.Asynchronous, cancellationToken);

        private async Task<int> LoadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
        {
            int recordsAffected;
            bool closeConnection = false;
            if (Connection == null)
            {
                throw new InvalidOperationException("Connection not set");
            }
            if (Connection.State != ConnectionState.Open)
            {
                closeConnection = true;
                Connection.Open();
            }
            if (string.IsNullOrWhiteSpace(FileName) && InfileStream != null)
            {
                if (!Local)
                {
                    throw new InvalidOperationException("Cannot use InfileStream when Local is not true.");
                }
                string streamKey = string.Format("{0}:{1}", LocalInfilePayload.InfileStreamPrefix, Guid.NewGuid());
                InfileStreams.Add(streamKey, InfileStream);
                FileName = streamKey;
            }
            try
            {
                string commandString = BuildSqlCommand();
                var cmd = new MySqlCommand(commandString, Connection)
                {
                    CommandTimeout = Timeout
                };
                recordsAffected = await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (closeConnection)
                {
                    Connection.Close();
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
