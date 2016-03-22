using System;

namespace MySql.Data
{
    internal static class Utility
    {
	    public static void Dispose<T>(ref T disposable)
			where T : class, IDisposable
		{
			if (disposable != null)
			{
				disposable.Dispose();
				disposable = null;
			}
		}
	}
}
