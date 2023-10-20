using System.Globalization;
using System.Text;

namespace MySqlConnector.Logging;

public class ConsoleLoggerProvider : IMySqlConnectorLoggerProvider
{
	public ConsoleLoggerProvider(MySqlConnectorLogLevel minimumLevel = MySqlConnectorLogLevel.Info, bool isColored = true)
	{
		if (minimumLevel < MySqlConnectorLogLevel.Trace || minimumLevel > MySqlConnectorLogLevel.Fatal)
			throw new ArgumentOutOfRangeException(nameof(minimumLevel), "minimumLevel must be between Trace and Fatal");

		m_minimumLevel = minimumLevel;
		m_isColored = isColored;
	}

	public IMySqlConnectorLogger CreateLogger(string name) => new ConsoleLogger(this, name);

	private sealed class ConsoleLogger(ConsoleLoggerProvider provider, string name) : IMySqlConnectorLogger
	{
		public bool IsEnabled(MySqlConnectorLogLevel level) => level >= Provider.m_minimumLevel && level <= MySqlConnectorLogLevel.Fatal;

		public void Log(MySqlConnectorLogLevel level, string message, object?[]? args = null, Exception? exception = null)
		{
			if (!IsEnabled(level))
				return;

			var sb = new StringBuilder();
			sb.Append(s_levels[(int) level]);
			sb.Append('\t');
			sb.Append(Name);
			sb.Append('\t');

			if (args is null || args.Length == 0)
				sb.Append(message);
			else
				sb.AppendFormat(CultureInfo.InvariantCulture, message, args);
			sb.AppendLine();

			if (exception is not null)
				sb.AppendLine(exception.ToString());

			if (Provider.m_isColored)
			{
				lock (Provider)
				{
					var oldColor = Console.ForegroundColor;
					Console.ForegroundColor = s_colors[(int) level];
					Console.Error.Write(sb.ToString());
					Console.ForegroundColor = oldColor;
				}
			}
			else
			{
				Console.Error.Write(sb.ToString());
			}
		}

		private static readonly string[] s_levels =
		{
			"",
			"[TRACE]",
			"[DEBUG]",
			"[INFO]",
			"[WARN]",
			"[ERROR]",
			"[FATAL]",
		};

		private static readonly ConsoleColor[] s_colors =
		{
			ConsoleColor.Black,
			ConsoleColor.DarkGray,
			ConsoleColor.Gray,
			ConsoleColor.White,
			ConsoleColor.Yellow,
			ConsoleColor.Red,
			ConsoleColor.Red,
		};

		private ConsoleLoggerProvider Provider { get; } = provider;
		private string Name { get; } = name;
	}

	private readonly MySqlConnectorLogLevel m_minimumLevel;
	private readonly bool m_isColored;
}
