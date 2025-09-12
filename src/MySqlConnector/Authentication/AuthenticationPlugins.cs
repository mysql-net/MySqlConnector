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
		ArgumentNullException.ThrowIfNull(plugin);
		ArgumentException.ThrowIfNullOrEmpty(plugin.Name);
		lock (s_lock)
			s_plugins.Add(plugin.Name, plugin);
	}

	internal static bool TryGetPlugin(string name, [NotNullWhen(true)] out IAuthenticationPlugin? plugin)
	{
		lock (s_lock)
			return s_plugins.TryGetValue(name, out plugin);
	}

#if NET9_0_OR_GREATER
	private static readonly Lock s_lock = new();
#else
	private static readonly object s_lock = new();
#endif
	private static readonly Dictionary<string, IAuthenticationPlugin> s_plugins = [];
}
