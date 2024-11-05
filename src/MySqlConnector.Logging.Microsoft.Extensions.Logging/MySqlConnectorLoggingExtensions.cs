using System;
using Microsoft.Extensions.Logging;

namespace MySqlConnector.Logging;

public static class MySqlConnectorLoggingExtensions
{
	[Obsolete("Use UseLoggerFactory or AddMySqlDataSource instead. See https://mysqlconnector.net/diagnostics/logging/.")]
	public static IServiceProvider UseMySqlConnectorLogging(this IServiceProvider services)
	{
		var loggerFactory = (ILoggerFactory) services.GetService(typeof(ILoggerFactory));
		if (loggerFactory is null)
			throw new InvalidOperationException("No ILoggerFactory service has been registered.");
		MySqlConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
		return services;
	}
}
