using System;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Events;

namespace MySqlConnector.Logging;

public sealed class SerilogLoggerProvider : IMySqlConnectorLoggerProvider
{
	public SerilogLoggerProvider()
	{
	}

	public IMySqlConnectorLogger CreateLogger(string name) => new SerilogLogger(name);

	private sealed class SerilogLogger : IMySqlConnectorLogger
	{
		public SerilogLogger(string name) => m_logger = Serilog.Log.ForContext("SourceContext", "MySqlConnector." + name);

		public bool IsEnabled(MySqlConnectorLogLevel level) => m_logger.IsEnabled(GetLevel(level));

		public void Log(MySqlConnectorLogLevel level, string message, object?[]? args = null, Exception? exception = null)
		{
			if (args is null || args.Length == 0)
			{
				m_logger.Write(GetLevel(level), exception, message);
			}
			else
			{
				// rewrite message as template
				var template = tokenReplacer.Replace(message, "$1{MySql$2$3}$4");
				m_logger.Write(GetLevel(level), exception, template, args);
			}
		}

		private static LogEventLevel GetLevel(MySqlConnectorLogLevel level) => level switch
		{
			MySqlConnectorLogLevel.Trace => LogEventLevel.Verbose,
			MySqlConnectorLogLevel.Debug => LogEventLevel.Debug,
			MySqlConnectorLogLevel.Info => LogEventLevel.Information,
			MySqlConnectorLogLevel.Warn => LogEventLevel.Warning,
			MySqlConnectorLogLevel.Error => LogEventLevel.Error,
			MySqlConnectorLogLevel.Fatal => LogEventLevel.Fatal,
			_ => throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid value for 'level'."),
		};

		private static readonly Regex tokenReplacer = new(@"((\w+)?\s?(?:=|:)?\s?'?)\{(?:\d+)(\:\w+)?\}('?)", RegexOptions.Compiled);

		private readonly ILogger m_logger;
	}
}
