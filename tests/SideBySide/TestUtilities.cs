using System.Diagnostics;
using Xunit;

namespace SideBySide
{
	public class TestUtilities
	{
		/// <summary>
		/// Asserts that two byte arrays are equal. This method is much faster than xUnit's <code>Assert.Equal</code>.
		/// </summary>
		/// <param name="expected">The expected byte array.</param>
		/// <param name="actual">The actual byte array.</param>
		public static void AssertEqual(byte[] expected, byte[] actual)
		{
			Assert.Equal(expected.Length, actual.Length);
			for (var i = 0; i < expected.Length; i++)
			{
				if (expected[i] != actual[i])
					Assert.Equal(expected[i], actual[i]);
			}
		}

		/// <summary>
		/// Asserts that <paramref name="stopwatch"/> is in the range [minimumMilliseconds, minimumMilliseconds + lengthMilliseconds].
		/// </summary>
		/// <remarks>This method applies a scaling factor for delays encountered under Continuous Integration environments.</remarks>
		public static void AssertDuration(Stopwatch stopwatch, int minimumMilliseconds, int lengthMilliseconds)
		{
			var elapsed = stopwatch.ElapsedMilliseconds;
			Assert.InRange(elapsed, minimumMilliseconds, minimumMilliseconds + lengthMilliseconds * AppConfig.TimeoutDelayFactor);
		}
	}
}
