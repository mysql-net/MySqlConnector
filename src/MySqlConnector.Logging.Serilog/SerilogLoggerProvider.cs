using System;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Events;

namespace MySqlConnector.Logging
{
    public sealed class SerilogLoggerProvider : IMySqlConnectorLoggerProvider
    {
		public SerilogLoggerProvider() { }

        public IMySqlConnectorLogger CreateLogger(string name)
        {
            return new SerilogLogger(name);
        }

        private class SerilogLogger : IMySqlConnectorLogger
        {
            public SerilogLogger(string name)
            {
                m_name = name;
                m_logger = Serilog.Log.ForContext("SourceContext", "MySqlConnector." + name);
            }

            public bool IsEnabled(MySqlConnectorLogLevel level) => m_logger.IsEnabled(GetLevel(level));

            public void Log(MySqlConnectorLogLevel level, string message, object[] args = null, Exception exception = null)
            {
                if (args == null || args.Length == 0)
                    m_logger.Write(GetLevel(level), exception, message);
                else
                {
					// rewrite message as template
					var template = tokenReplacer.Replace(message, "$1{MySql$2$3}$4");
					m_logger.Write(GetLevel(level), exception, template, args);
                }
            }

            private static LogEventLevel GetLevel(MySqlConnectorLogLevel level)
            {
                switch (level)
                {
                    case MySqlConnectorLogLevel.Trace:
                        return LogEventLevel.Verbose;
                    case MySqlConnectorLogLevel.Debug:
                        return LogEventLevel.Debug;
                    case MySqlConnectorLogLevel.Info:
                        return LogEventLevel.Information;
                    case MySqlConnectorLogLevel.Warn:
                        return LogEventLevel.Warning;
                    case MySqlConnectorLogLevel.Error:
                        return LogEventLevel.Error;
                    case MySqlConnectorLogLevel.Fatal:
                        return LogEventLevel.Fatal;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid value for 'level'.");
                }
            }

			static readonly Regex tokenReplacer = new Regex(@"((\w+)?\s?(?:=|:)?\s?'?)\{(?:\d+)(\:\w+)?\}('?)", RegexOptions.Compiled);

			readonly ILogger m_logger;
            readonly string m_name;
        }

    }
}
