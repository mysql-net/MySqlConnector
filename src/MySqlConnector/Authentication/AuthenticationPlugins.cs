using System.Diagnostics.CodeAnalysis;

namespace MySqlConnector.Authentication;

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
#if NET6_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(plugin);
#else
		if (plugin is null)
			throw new ArgumentNullException(nameof(plugin));
#endif
#if NET8_0_OR_GREATER
		ArgumentException.ThrowIfNullOrEmpty(plugin.Name);
#else
		if (string.IsNullOrEmpty(plugin.Name))
			throw new ArgumentException("Invalid plugin name.", nameof(plugin));
#endif
		lock (s_lock)
			s_plugins.Add(plugin.Name, plugin);
	}

	internal static bool TryGetPlugin(string name, [NotNullWhen(true)] out IAuthenticationPlugin? plugin)
	{
		lock (s_lock)
			return s_plugins.TryGetValue(name, out plugin);
	}

	private static readonly object s_lock = new();
	private static readonly Dictionary<string, IAuthenticationPlugin> s_plugins = new();
}
