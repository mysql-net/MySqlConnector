using System;
using System.Collections.Generic;

namespace MySqlConnector.Authentication
{
	/// <summary>
	/// A registry of known authentication plugins.
	/// </summary>
	public static class AuthenticationPlugins
	{
		/// <summary>
		/// Registers the specified authentication plugin. The name of this plugin must be unique.
		/// </summary>
		/// <param name="plugin">The authentication plugin.</param>
		public static void Register(IAuthenticationPlugin plugin)
		{
			if (plugin is null)
				throw new ArgumentNullException(nameof(plugin));
			if (string.IsNullOrEmpty(plugin.Name))
				throw new ArgumentException("Invalid plugin name.", nameof(plugin));

			lock (s_lock)
				s_plugins.Add(plugin.Name, plugin);
		}

		internal static bool TryGetPlugin(string name, out IAuthenticationPlugin plugin)
		{
			lock (s_lock)
				return s_plugins.TryGetValue(name, out plugin);
		}

		static readonly object s_lock = new object();
		static readonly Dictionary<string, IAuthenticationPlugin> s_plugins = new Dictionary<string, IAuthenticationPlugin>();
	}
}
