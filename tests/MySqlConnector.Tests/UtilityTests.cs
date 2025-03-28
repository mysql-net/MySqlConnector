using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MySqlConnector.Utilities;

namespace MySqlConnector.Tests;

public class UtilityTests
{
	[Theory]
	[InlineData("mariadb://host.example.com:1234/?user=user@host", "host.example.com", 1234, "user@host")]
	[InlineData("mariadb://user%40host:password@host.example.com:1234/", "host.example.com", 1234, "user@host")]
	[InlineData("mariadb://host.example.com:1234/?user=user@host&ttl=60", "host.example.com", 1234, "user@host")]
	[InlineData("mariadb://someuser:password@host.example.com:1234/?user=user@host&ttl=60\n", "host.example.com", 1234, "someuser")]
	[InlineData("mysql://[2001:4860:4860::8888]:1234/?user=abcd", "2001:4860:4860::8888", 1234, "abcd")]
	[InlineData("mysql://[2001:4860:4860::8888]:1234/?user=abcd\n", "2001:4860:4860::8888", 1234, "abcd")]
	[InlineData("mysql://[2001:4860:4860::8888]:1234/?user=abcd&ttl=60", "2001:4860:4860::8888", 1234, "abcd")]
	[InlineData("mysql://[2001:4860:4860::8888]:1234/?user=abcd&ttl=60\n", "2001:4860:4860::8888", 1234, "abcd")]
	public void ParseRedirectionHeader(string input, string expectedHost, int expectedPort, string expectedUser)
	{
		Assert.True(Utility.TryParseRedirectionHeader(input, null, out var host, out var port, out var user));
		Assert.Equal(expectedHost, host);
		Assert.Equal(expectedPort, port);
		Assert.Equal(expectedUser, user);
	}

	[Theory]
	[InlineData("")]
	[InlineData("not formated")]
	[InlineData("mysql")]
	[InlineData("mysql://[host.example.com")]
	[InlineData("mysql://host.example.com:-1/user=user@host")]
	[InlineData("mysql://[host.example.com]:123/user=abcd")]
	public void ParseRedirectionHeaderFails(string input)
	{
		Assert.False(Utility.TryParseRedirectionHeader(input, null, out _, out _, out _));
	}

	[Theory]
	[InlineData("00:00:00", "00:00:00")]
	[InlineData("00:00:01", "00:00:01")]
	[InlineData("00:01:00", "00:01:00")]
	[InlineData("00:12:34", "00:12:34")]
	[InlineData("01:00:00", "01:00:00")]
	[InlineData("12:34:56", "12:34:56")]
	[InlineData("-00:00:01", "-00:00:01")]
	[InlineData("-00:01:00", "-00:01:00")]
	[InlineData("-00:12:34", "-00:12:34")]
	[InlineData("-01:00:00", "-01:00:00")]
	[InlineData("-12:34:56", "-12:34:56")]
	[InlineData("00:00:00.1", "00:00:00.1")]
	[InlineData("00:00:00.12", "00:00:00.12")]
	[InlineData("00:00:00.123", "00:00:00.123")]
	[InlineData("00:00:00.1234", "00:00:00.1234")]
	[InlineData("00:00:00.12345", "00:00:00.12345")]
	[InlineData("00:00:00.123456", "00:00:00.123456")]
	[InlineData("-00:00:00.1", "-00:00:00.1")]
	[InlineData("-00:00:00.12", "-00:00:00.12")]
	[InlineData("-00:00:00.123", "-00:00:00.123")]
	[InlineData("-00:00:00.1234", "-00:00:00.1234")]
	[InlineData("-00:00:00.12345", "-00:00:00.12345")]
	[InlineData("-00:00:00.123456", "-00:00:00.123456")]
	[InlineData("838:59:59", "34.22:59:59")]
	[InlineData("838:59:59.999999", "34.22:59:59.999999")]
	[InlineData("-838:59:59", "-34.22:59:59")]
	[InlineData("-838:59:59.999999", "-34.22:59:59.999999")]
	public void ParseTimeSpan(string input, string expectedString)
	{
		var expected = TimeSpan.ParseExact(expectedString, "c", CultureInfo.InvariantCulture);
		var actual = Utility.ParseTimeSpan(Encoding.ASCII.GetBytes(input));
		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("0")]
	[InlineData("0:0:0")]
	[InlineData("--01:00:00")]
	[InlineData("00-00-00")]
	[InlineData("00:00:60")]
	[InlineData("00:60:00")]
	[InlineData("999:00:00")]
	[InlineData("00:00:00.1234567")]
	public void ParseTimeSpanFails(string input)
	{
		Assert.Throws<FormatException>(() => Utility.ParseTimeSpan(Encoding.ASCII.GetBytes(input)));
	}

	[Theory]
	[InlineData("", "")]
	[InlineData("pre\n", "")]
	[InlineData("", "\npost")]
	[InlineData("pre\n", "\npost")]
	public void DecodePublicKey(string pre, string post)
	{
#if NET5_0_OR_GREATER
		using var rsa = RSA.Create();
		Utility.LoadRsaParameters(pre + c_publicKey + post, rsa);
		var parameters = rsa.ExportParameters(false);
#else
		var parameters = Utility.GetRsaParameters(pre + c_publicKey + post);
#endif
		Assert.Equal(s_modulus, parameters.Modulus);
		Assert.Equal(s_exponent, parameters.Exponent);
	}

	[Theory]
	[InlineData("", "")]
	[InlineData("pre\n", "")]
	[InlineData("", "\npost")]
	[InlineData("pre\n", "\npost")]
	public void DecodePrivateKey(string pre, string post)
	{
#if NET5_0_OR_GREATER
		using var rsa = RSA.Create();
		Utility.LoadRsaParameters(pre + c_privateKey + post, rsa);
		var parameters = rsa.ExportParameters(true);
#else
		var parameters = Utility.GetRsaParameters(pre + c_privateKey + post);
#endif
		Assert.Equal(s_modulus, parameters.Modulus);
		Assert.Equal(s_exponent, parameters.Exponent);
		Assert.Equal(s_d, parameters.D);
		Assert.Equal(s_p, parameters.P);
		Assert.Equal(s_q, parameters.Q);
		Assert.Equal(s_dp, parameters.DP);
		Assert.Equal(s_dq, parameters.DQ);
		Assert.Equal(s_iq, parameters.InverseQ);
	}

	private const string c_publicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwmlDX62hQAQNvSJZ/HAO
UjCbAiEPQquEyPpxjqDxyx1fVxL93U1au50xGk4sad4OH+GSZCChqj3kvwJhXc52
iHdBzjQbucGYlC1wLNMc1F+H89vMjEq1ZexsRDWQSrgL1I6i9Mn5NFgS563yPBpO
yfYyGWCrL5w7yI+we3MCwy2q8JIOyZegzh76W+f3F9pFdnfjQO+vjPyNupnTEDtY
bGjFYaqYITNncZ8cL1LYVwuUXW0PW8bflEdFd/Br8fzCgK4IVdSm6OVPzSZataw4
2jcX2tNT8f3P3ClAvL1V3j4EdCHyYNrKJqwZ7nTtjqBBPqNmNicR3eM8suGjMWym
LwIDAQAB
-----END PUBLIC KEY-----";

	private const string c_privateKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEAwmlDX62hQAQNvSJZ/HAOUjCbAiEPQquEyPpxjqDxyx1fVxL9
3U1au50xGk4sad4OH+GSZCChqj3kvwJhXc52iHdBzjQbucGYlC1wLNMc1F+H89vM
jEq1ZexsRDWQSrgL1I6i9Mn5NFgS563yPBpOyfYyGWCrL5w7yI+we3MCwy2q8JIO
yZegzh76W+f3F9pFdnfjQO+vjPyNupnTEDtYbGjFYaqYITNncZ8cL1LYVwuUXW0P
W8bflEdFd/Br8fzCgK4IVdSm6OVPzSZataw42jcX2tNT8f3P3ClAvL1V3j4EdCHy
YNrKJqwZ7nTtjqBBPqNmNicR3eM8suGjMWymLwIDAQABAoIBABGzYdYBShA5DkMt
MIJCdZtYH5TnC6unUDS4UMSKtNkoeHjGGuUeWOeCHWlmurZ86E5QzHJfYjjM39ng
Tpsq5DHyocQzjF6yWMENDgyWwCY5+HfkiFAxsShxiT4Ann2fGjylLAMbrJvudPIx
LT/5qTjMOK2E1rFECVFue8QRqau5gNSoh+8sdlFkPuU3f/Em18CA65apGk24r8nl
okN6bOuf84py3efS62DWAd0lQItxug1JVTodm2BxLDev+9k873fxQ+l/6Szcdtw0
SP9PtL6TgsMUQsbkAfWw3u1kgrkcFwooiWbCRaNOvGG018xH3IaOH+6ODQ9Rsaxi
79xtkIECgYEA+mMTGfMTesBwZ63oBSZwaJ8w+waeRf848lajNU+uISCURIBCYTnD
66mP0mGI6WpiQvAYVd5Qcx76cOUlcyhvKUDgCRq2EfSx7IklZn1Bx1P6Oljwsidz
6GNJBhXxTDWdM+UoPr8vn+Qz6c5vkRitldT1EtaSvdrfZKi07H7YGkECgYEAxsT0
GRp0+6EHPYqAmm/FYMtoMhTKnSFZfKWsC2gOjOKCd2y8wqaz8ovkyN/5e5KC5iCE
y+BGRAq010hDKXjnyuSP7FIqb9pYjarrwF5PwXCal87k/3DsoSrt2HqhpRx5Ko68
dKNFICcU4bkWgCaBwhcmu4MEiPsLsgZJiJrARG8CgYEAqRzRge1Tcs0XHm+vDgtw
ULl0c5d8vvoqlEM/6Hnwuh8vBTU7oD9LvJfBs+58cmqQA3X2eci1vYtmy2l5adYd
fld6+as43dfPdFsND7P6AY8Oqun3Z9yNxJ+KarnXSAnOK4bTL84hdHTFO11arT1l
wJUdEaQraF+0EmCAElt5ygECgYEAmX/yDgzbeJNjlwgFxr44TEbpIXvi+LOPeu4q
Tei/C9fE71s+7od0ggO828/zx5Velz9XrmZ1fQhTncmFbFqdjpcx+kt90EFrj4QG
admrd/RwvnmdaRMY+mG/qiUR/gNeIxp1uRF5SZpEywh3suGJ5Yyhcb99WhedcY7f
bKotfusCgYAvkYRYq0eu5pK85BO6nN1m/5GolEG/1lQbbZLAZ8YII6RtJKHBvcvF
r06WOUZKRNcQjzV29cuG4pqtMs0swLhKMbCs+Pqr6w6KfZINtVyLNtbDVo9rwFRc
joMn7CFmwV9EYjPTxwkByKs5a8AbNYJDSfNCa3KoHTFjOvVMPec90Q==
-----END RSA PRIVATE KEY-----";

	private static readonly byte[] s_modulus = [0xC2, 0x69, 0x43, 0x5F, 0xAD, 0xA1, 0x40, 0x04, 0x0D, 0xBD, 0x22, 0x59, 0xFC, 0x70, 0x0E, 0x52, 0x30, 0x9B, 0x02, 0x21, 0x0F, 0x42, 0xAB, 0x84, 0xC8, 0xFA, 0x71, 0x8E, 0xA0, 0xF1, 0xCB, 0x1D, 0x5F, 0x57, 0x12, 0xFD, 0xDD, 0x4D, 0x5A, 0xBB, 0x9D, 0x31, 0x1A, 0x4E, 0x2C, 0x69, 0xDE, 0x0E, 0x1F, 0xE1, 0x92, 0x64, 0x20, 0xA1, 0xAA, 0x3D, 0xE4, 0xBF, 0x02, 0x61, 0x5D, 0xCE, 0x76, 0x88, 0x77, 0x41, 0xCE, 0x34, 0x1B, 0xB9, 0xC1, 0x98, 0x94, 0x2D, 0x70, 0x2C, 0xD3, 0x1C, 0xD4, 0x5F, 0x87, 0xF3, 0xDB, 0xCC, 0x8C, 0x4A, 0xB5, 0x65, 0xEC, 0x6C, 0x44, 0x35, 0x90, 0x4A, 0xB8, 0x0B, 0xD4, 0x8E, 0xA2, 0xF4, 0xC9, 0xF9, 0x34, 0x58, 0x12, 0xE7, 0xAD, 0xF2, 0x3C, 0x1A, 0x4E, 0xC9, 0xF6, 0x32, 0x19, 0x60, 0xAB, 0x2F, 0x9C, 0x3B, 0xC8, 0x8F, 0xB0, 0x7B, 0x73, 0x02, 0xC3, 0x2D, 0xAA, 0xF0, 0x92, 0x0E, 0xC9, 0x97, 0xA0, 0xCE, 0x1E, 0xFA, 0x5B, 0xE7, 0xF7, 0x17, 0xDA, 0x45, 0x76, 0x77, 0xE3, 0x40, 0xEF, 0xAF, 0x8C, 0xFC, 0x8D, 0xBA, 0x99, 0xD3, 0x10, 0x3B, 0x58, 0x6C, 0x68, 0xC5, 0x61, 0xAA, 0x98, 0x21, 0x33, 0x67, 0x71, 0x9F, 0x1C, 0x2F, 0x52, 0xD8, 0x57, 0x0B, 0x94, 0x5D, 0x6D, 0x0F, 0x5B, 0xC6, 0xDF, 0x94, 0x47, 0x45, 0x77, 0xF0, 0x6B, 0xF1, 0xFC, 0xC2, 0x80, 0xAE, 0x08, 0x55, 0xD4, 0xA6, 0xE8, 0xE5, 0x4F, 0xCD, 0x26, 0x5A, 0xB5, 0xAC, 0x38, 0xDA, 0x37, 0x17, 0xDA, 0xD3, 0x53, 0xF1, 0xFD, 0xCF, 0xDC, 0x29, 0x40, 0xBC, 0xBD, 0x55, 0xDE, 0x3E, 0x04, 0x74, 0x21, 0xF2, 0x60, 0xDA, 0xCA, 0x26, 0xAC, 0x19, 0xEE, 0x74, 0xED, 0x8E, 0xA0, 0x41, 0x3E, 0xA3, 0x66, 0x36, 0x27, 0x11, 0xDD, 0xE3, 0x3C, 0xB2, 0xE1, 0xA3, 0x31, 0x6C, 0xA6, 0x2F];
	private static readonly byte[] s_exponent = [0x01, 0x00, 0x01];
	private static readonly byte[] s_d = [0x11, 0xB3, 0x61, 0xD6, 0x01, 0x4A, 0x10, 0x39, 0x0E, 0x43, 0x2D, 0x30, 0x82, 0x42, 0x75, 0x9B, 0x58, 0x1F, 0x94, 0xE7, 0x0B, 0xAB, 0xA7, 0x50, 0x34, 0xB8, 0x50, 0xC4, 0x8A, 0xB4, 0xD9, 0x28, 0x78, 0x78, 0xC6, 0x1A, 0xE5, 0x1E, 0x58, 0xE7, 0x82, 0x1D, 0x69, 0x66, 0xBA, 0xB6, 0x7C, 0xE8, 0x4E, 0x50, 0xCC, 0x72, 0x5F, 0x62, 0x38, 0xCC, 0xDF, 0xD9, 0xE0, 0x4E, 0x9B, 0x2A, 0xE4, 0x31, 0xF2, 0xA1, 0xC4, 0x33, 0x8C, 0x5E, 0xB2, 0x58, 0xC1, 0x0D, 0x0E, 0x0C, 0x96, 0xC0, 0x26, 0x39, 0xF8, 0x77, 0xE4, 0x88, 0x50, 0x31, 0xB1, 0x28, 0x71, 0x89, 0x3E, 0x00, 0x9E, 0x7D, 0x9F, 0x1A, 0x3C, 0xA5, 0x2C, 0x03, 0x1B, 0xAC, 0x9B, 0xEE, 0x74, 0xF2, 0x31, 0x2D, 0x3F, 0xF9, 0xA9, 0x38, 0xCC, 0x38, 0xAD, 0x84, 0xD6, 0xB1, 0x44, 0x09, 0x51, 0x6E, 0x7B, 0xC4, 0x11, 0xA9, 0xAB, 0xB9, 0x80, 0xD4, 0xA8, 0x87, 0xEF, 0x2C, 0x76, 0x51, 0x64, 0x3E, 0xE5, 0x37, 0x7F, 0xF1, 0x26, 0xD7, 0xC0, 0x80, 0xEB, 0x96, 0xA9, 0x1A, 0x4D, 0xB8, 0xAF, 0xC9, 0xE5, 0xA2, 0x43, 0x7A, 0x6C, 0xEB, 0x9F, 0xF3, 0x8A, 0x72, 0xDD, 0xE7, 0xD2, 0xEB, 0x60, 0xD6, 0x01, 0xDD, 0x25, 0x40, 0x8B, 0x71, 0xBA, 0x0D, 0x49, 0x55, 0x3A, 0x1D, 0x9B, 0x60, 0x71, 0x2C, 0x37, 0xAF, 0xFB, 0xD9, 0x3C, 0xEF, 0x77, 0xF1, 0x43, 0xE9, 0x7F, 0xE9, 0x2C, 0xDC, 0x76, 0xDC, 0x34, 0x48, 0xFF, 0x4F, 0xB4, 0xBE, 0x93, 0x82, 0xC3, 0x14, 0x42, 0xC6, 0xE4, 0x01, 0xF5, 0xB0, 0xDE, 0xED, 0x64, 0x82, 0xB9, 0x1C, 0x17, 0x0A, 0x28, 0x89, 0x66, 0xC2, 0x45, 0xA3, 0x4E, 0xBC, 0x61, 0xB4, 0xD7, 0xCC, 0x47, 0xDC, 0x86, 0x8E, 0x1F, 0xEE, 0x8E, 0x0D, 0x0F, 0x51, 0xB1, 0xAC, 0x62, 0xEF, 0xDC, 0x6D, 0x90, 0x81];
	private static readonly byte[] s_p = [0xFA, 0x63, 0x13, 0x19, 0xF3, 0x13, 0x7A, 0xC0, 0x70, 0x67, 0xAD, 0xE8, 0x05, 0x26, 0x70, 0x68, 0x9F, 0x30, 0xFB, 0x06, 0x9E, 0x45, 0xFF, 0x38, 0xF2, 0x56, 0xA3, 0x35, 0x4F, 0xAE, 0x21, 0x20, 0x94, 0x44, 0x80, 0x42, 0x61, 0x39, 0xC3, 0xEB, 0xA9, 0x8F, 0xD2, 0x61, 0x88, 0xE9, 0x6A, 0x62, 0x42, 0xF0, 0x18, 0x55, 0xDE, 0x50, 0x73, 0x1E, 0xFA, 0x70, 0xE5, 0x25, 0x73, 0x28, 0x6F, 0x29, 0x40, 0xE0, 0x09, 0x1A, 0xB6, 0x11, 0xF4, 0xB1, 0xEC, 0x89, 0x25, 0x66, 0x7D, 0x41, 0xC7, 0x53, 0xFA, 0x3A, 0x58, 0xF0, 0xB2, 0x27, 0x73, 0xE8, 0x63, 0x49, 0x06, 0x15, 0xF1, 0x4C, 0x35, 0x9D, 0x33, 0xE5, 0x28, 0x3E, 0xBF, 0x2F, 0x9F, 0xE4, 0x33, 0xE9, 0xCE, 0x6F, 0x91, 0x18, 0xAD, 0x95, 0xD4, 0xF5, 0x12, 0xD6, 0x92, 0xBD, 0xDA, 0xDF, 0x64, 0xA8, 0xB4, 0xEC, 0x7E, 0xD8, 0x1A, 0x41];
	private static readonly byte[] s_q = [0xC6, 0xC4, 0xF4, 0x19, 0x1A, 0x74, 0xFB, 0xA1, 0x07, 0x3D, 0x8A, 0x80, 0x9A, 0x6F, 0xC5, 0x60, 0xCB, 0x68, 0x32, 0x14, 0xCA, 0x9D, 0x21, 0x59, 0x7C, 0xA5, 0xAC, 0x0B, 0x68, 0x0E, 0x8C, 0xE2, 0x82, 0x77, 0x6C, 0xBC, 0xC2, 0xA6, 0xB3, 0xF2, 0x8B, 0xE4, 0xC8, 0xDF, 0xF9, 0x7B, 0x92, 0x82, 0xE6, 0x20, 0x84, 0xCB, 0xE0, 0x46, 0x44, 0x0A, 0xB4, 0xD7, 0x48, 0x43, 0x29, 0x78, 0xE7, 0xCA, 0xE4, 0x8F, 0xEC, 0x52, 0x2A, 0x6F, 0xDA, 0x58, 0x8D, 0xAA, 0xEB, 0xC0, 0x5E, 0x4F, 0xC1, 0x70, 0x9A, 0x97, 0xCE, 0xE4, 0xFF, 0x70, 0xEC, 0xA1, 0x2A, 0xED, 0xD8, 0x7A, 0xA1, 0xA5, 0x1C, 0x79, 0x2A, 0x8E, 0xBC, 0x74, 0xA3, 0x45, 0x20, 0x27, 0x14, 0xE1, 0xB9, 0x16, 0x80, 0x26, 0x81, 0xC2, 0x17, 0x26, 0xBB, 0x83, 0x04, 0x88, 0xFB, 0x0B, 0xB2, 0x06, 0x49, 0x88, 0x9A, 0xC0, 0x44, 0x6F];
	private static readonly byte[] s_dp = [0xA9, 0x1C, 0xD1, 0x81, 0xED, 0x53, 0x72, 0xCD, 0x17, 0x1E, 0x6F, 0xAF, 0x0E, 0x0B, 0x70, 0x50, 0xB9, 0x74, 0x73, 0x97, 0x7C, 0xBE, 0xFA, 0x2A, 0x94, 0x43, 0x3F, 0xE8, 0x79, 0xF0, 0xBA, 0x1F, 0x2F, 0x05, 0x35, 0x3B, 0xA0, 0x3F, 0x4B, 0xBC, 0x97, 0xC1, 0xB3, 0xEE, 0x7C, 0x72, 0x6A, 0x90, 0x03, 0x75, 0xF6, 0x79, 0xC8, 0xB5, 0xBD, 0x8B, 0x66, 0xCB, 0x69, 0x79, 0x69, 0xD6, 0x1D, 0x7E, 0x57, 0x7A, 0xF9, 0xAB, 0x38, 0xDD, 0xD7, 0xCF, 0x74, 0x5B, 0x0D, 0x0F, 0xB3, 0xFA, 0x01, 0x8F, 0x0E, 0xAA, 0xE9, 0xF7, 0x67, 0xDC, 0x8D, 0xC4, 0x9F, 0x8A, 0x6A, 0xB9, 0xD7, 0x48, 0x09, 0xCE, 0x2B, 0x86, 0xD3, 0x2F, 0xCE, 0x21, 0x74, 0x74, 0xC5, 0x3B, 0x5D, 0x5A, 0xAD, 0x3D, 0x65, 0xC0, 0x95, 0x1D, 0x11, 0xA4, 0x2B, 0x68, 0x5F, 0xB4, 0x12, 0x60, 0x80, 0x12, 0x5B, 0x79, 0xCA, 0x01];
	private static readonly byte[] s_dq = [0x99, 0x7F, 0xF2, 0x0E, 0x0C, 0xDB, 0x78, 0x93, 0x63, 0x97, 0x08, 0x05, 0xC6, 0xBE, 0x38, 0x4C, 0x46, 0xE9, 0x21, 0x7B, 0xE2, 0xF8, 0xB3, 0x8F, 0x7A, 0xEE, 0x2A, 0x4D, 0xE8, 0xBF, 0x0B, 0xD7, 0xC4, 0xEF, 0x5B, 0x3E, 0xEE, 0x87, 0x74, 0x82, 0x03, 0xBC, 0xDB, 0xCF, 0xF3, 0xC7, 0x95, 0x5E, 0x97, 0x3F, 0x57, 0xAE, 0x66, 0x75, 0x7D, 0x08, 0x53, 0x9D, 0xC9, 0x85, 0x6C, 0x5A, 0x9D, 0x8E, 0x97, 0x31, 0xFA, 0x4B, 0x7D, 0xD0, 0x41, 0x6B, 0x8F, 0x84, 0x06, 0x69, 0xD9, 0xAB, 0x77, 0xF4, 0x70, 0xBE, 0x79, 0x9D, 0x69, 0x13, 0x18, 0xFA, 0x61, 0xBF, 0xAA, 0x25, 0x11, 0xFE, 0x03, 0x5E, 0x23, 0x1A, 0x75, 0xB9, 0x11, 0x79, 0x49, 0x9A, 0x44, 0xCB, 0x08, 0x77, 0xB2, 0xE1, 0x89, 0xE5, 0x8C, 0xA1, 0x71, 0xBF, 0x7D, 0x5A, 0x17, 0x9D, 0x71, 0x8E, 0xDF, 0x6C, 0xAA, 0x2D, 0x7E, 0xEB];
	private static readonly byte[] s_iq = [0x2F, 0x91, 0x84, 0x58, 0xAB, 0x47, 0xAE, 0xE6, 0x92, 0xBC, 0xE4, 0x13, 0xBA, 0x9C, 0xDD, 0x66, 0xFF, 0x91, 0xA8, 0x94, 0x41, 0xBF, 0xD6, 0x54, 0x1B, 0x6D, 0x92, 0xC0, 0x67, 0xC6, 0x08, 0x23, 0xA4, 0x6D, 0x24, 0xA1, 0xC1, 0xBD, 0xCB, 0xC5, 0xAF, 0x4E, 0x96, 0x39, 0x46, 0x4A, 0x44, 0xD7, 0x10, 0x8F, 0x35, 0x76, 0xF5, 0xCB, 0x86, 0xE2, 0x9A, 0xAD, 0x32, 0xCD, 0x2C, 0xC0, 0xB8, 0x4A, 0x31, 0xB0, 0xAC, 0xF8, 0xFA, 0xAB, 0xEB, 0x0E, 0x8A, 0x7D, 0x92, 0x0D, 0xB5, 0x5C, 0x8B, 0x36, 0xD6, 0xC3, 0x56, 0x8F, 0x6B, 0xC0, 0x54, 0x5C, 0x8E, 0x83, 0x27, 0xEC, 0x21, 0x66, 0xC1, 0x5F, 0x44, 0x62, 0x33, 0xD3, 0xC7, 0x09, 0x01, 0xC8, 0xAB, 0x39, 0x6B, 0xC0, 0x1B, 0x35, 0x82, 0x43, 0x49, 0xF3, 0x42, 0x6B, 0x72, 0xA8, 0x1D, 0x31, 0x63, 0x3A, 0xF5, 0x4C, 0x3D, 0xE7, 0x3D, 0xD1];
}
