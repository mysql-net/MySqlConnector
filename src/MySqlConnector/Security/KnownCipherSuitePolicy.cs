#if NET5_0
using System.Net.Security;

namespace MySqlConnector.Core
{
	internal static class KnownCipherSuitePolicy
	{
		public static readonly CipherSuitesPolicy Net50_Aurora2 = new CipherSuitesPolicy(new[] {
			// Default .NET5 policy (TLS 1.3) [all currently unsupported by Aurora 2 (MySQL 5.7)]

			TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384, 
			TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256, 
			TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,   
			TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,   
			TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384, 
			TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
			TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384,
			TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256,

			// Additional Ciphers
			TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
			TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384,
			TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384,
			TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
		});
	}
}

#endif
