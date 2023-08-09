#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_1_OR_GREATER
#pragma warning disable SA1005 // Single line comments should begin with single space
#pragma warning disable SA1120 // Comments should contain text
#pragma warning disable SA1512 // Single-line comments should not be followed by blank line
#pragma warning disable SA1515 // Single-line comment should be preceded by blank line

// copied from https://github.com/mono/mono/blob/master/mcs/class/Mono.Posix/Mono.Unix/UnixEndPoint.cs

//
// Mono.Unix.UnixEndPoint: EndPoint derived class for AF_UNIX family sockets.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2003 Ximian, Inc (http://www.ximian.com)
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Text;

namespace System.Net.Sockets;

internal sealed class UnixDomainSocketEndPoint : EndPoint
{
	public UnixDomainSocketEndPoint(string filename)
	{
		if (filename is null)
			throw new ArgumentNullException(nameof(filename));
		if (filename.Length == 0)
			throw new ArgumentException("Cannot be empty.", nameof(filename));
		Filename = filename;
	}

	private UnixDomainSocketEndPoint() => Filename = "";

	public string Filename { get; }

	public override AddressFamily AddressFamily => AddressFamily.Unix;

	public override EndPoint Create(SocketAddress socketAddress)
	{
		if (socketAddress.Size == 2) {
			// Empty filename.
			// Probably from RemoteEndPoint which on linux does not return the file name.
			return new UnixDomainSocketEndPoint();
		}
		var size = socketAddress.Size - 2;
		var bytes = new byte[size];
		for (var i = 0; i < bytes.Length; i++) {
			bytes[i] = socketAddress[i + 2];
			// There may be junk after the null terminator, so ignore it all.
			if (bytes[i] == 0) {
				size = i;
				break;
			}
		}

		return new UnixDomainSocketEndPoint(Encoding.UTF8.GetString(bytes, 0, size));
	}

	public override SocketAddress Serialize()
	{
		var bytes = Encoding.UTF8.GetBytes(Filename);
		var sa = new SocketAddress(AddressFamily, 2 + bytes.Length + 1);
		// sa [0] -> family low byte, sa [1] -> family high byte
		for (var i = 0; i < bytes.Length; i++)
			sa[2 + i] = bytes[i];

		//NULL suffix for non-abstract path
		sa[2 + bytes.Length] = 0;

		return sa;
	}

	public override string ToString() => Filename;

	public override int GetHashCode() => Filename.GetHashCode ();

	public override bool Equals(object? obj) => obj is UnixDomainSocketEndPoint other && Filename == other.Filename;
}
#endif
