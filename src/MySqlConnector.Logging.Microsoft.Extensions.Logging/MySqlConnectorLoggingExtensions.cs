using System;
using Microsoft.Extensions.Logging;
using MySqlConnector.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class MySqlConnectorLoggingExtensions
{
	public static IServiceProvider UseMySqlConnectorLogging(this IServiceProvider services)
	{
		var loggerFactory = (ILoggerFactory) services.GetService(typeof(ILoggerFactory));
		if (loggerFactory is null)
			throw new InvalidOperationException("No ILoggerFactory service has been registered.");
		MySqlConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
		return services;
	}
}
