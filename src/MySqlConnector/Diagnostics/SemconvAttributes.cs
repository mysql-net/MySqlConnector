// src/MySqlConnector/Diagnostics/SemconvAttributes.cs
using System.Diagnostics;

namespace MySqlConnector.Diagnostics
{
    internal static class SemconvAttributes
    {
        public static void SetNetworkPeer(Activity activity, string host, int? port, string sockFamily = null, string transport = null)
        {
            if (activity == null) return;

            if (SemconvConfig.EmitOld)
            {
                if (!string.IsNullOrEmpty(host)) activity.SetTag("net.peer.name", host);
                if (port.HasValue) activity.SetTag("net.peer.port", port.Value);
                if (!string.IsNullOrEmpty(sockFamily)) activity.SetTag("net.sock.family", sockFamily);
                if (!string.IsNullOrEmpty(transport)) activity.SetTag("net.transport", transport);
                // net.sock.peer.addr / net.sock.peer.port / net.sock.* could be set if IP addr/socket known
            }

            if (SemconvConfig.EmitNew)
            {
                if (!string.IsNullOrEmpty(host)) activity.SetTag("server.address", host);
                if (port.HasValue) activity.SetTag("server.port", port.Value);
                // network.peer.address = host or IP
                if (!string.IsNullOrEmpty(host)) activity.SetTag("network.peer.address", host);
                if (port.HasValue) activity.SetTag("network.peer.port", port.Value);
                // canonicalize transport: map 'ip_tcp' -> 'tcp', 'ip_udp' -> 'udp', etc.
                if (!string.IsNullOrEmpty(transport))
                    activity.SetTag("network.transport", MapTransportToSemconv(transport));
                if (!string.IsNullOrEmpty(sockFamily))
                    activity.SetTag("network.type", MapSockFamilyToNetworkType(sockFamily));
            }
        }

        public static void SetDbAttributes(Activity activity, string dbSystem, string dbUser, string dbName, string dbInstanceId = null)
        {
            if (activity == null) return;
            if (SemconvConfig.EmitOld)
            {
                if (!string.IsNullOrEmpty(dbSystem)) activity.SetTag("db.system", dbSystem);
                if (!string.IsNullOrEmpty(dbUser)) activity.SetTag("db.user", dbUser);
                if (!string.IsNullOrEmpty(dbName)) activity.SetTag("db.name", dbName);
                // legacy didn't have db.instance.id
            }
            if (SemconvConfig.EmitNew)
            {
                if (!string.IsNullOrEmpty(dbSystem)) activity.SetTag("db.system", dbSystem);
                if (!string.IsNullOrEmpty(dbUser)) activity.SetTag("db.user", dbUser);
                if (!string.IsNullOrEmpty(dbName)) activity.SetTag("db.name", dbName);
                if (!string.IsNullOrEmpty(dbInstanceId)) activity.SetTag("db.instance.id", dbInstanceId);
            }
        }

        private static string MapTransportToSemconv(string transport)
        {
            if (string.IsNullOrEmpty(transport)) return transport;
            switch (transport.ToLowerInvariant())
            {
                case "ip_tcp":
                case "tcp":
                    return "tcp";
                case "ip_udp":
                case "udp":
                    return "udp";
                default:
                    return transport;
            }
        }

        private static string MapSockFamilyToNetworkType(string family)
        {
            if (string.IsNullOrEmpty(family)) return family;
            switch (family.ToLowerInvariant())
            {
                case "inet":
                case "inet4":
                case "ipv4":
                    return "ipv4";
                case "inet6":
                case "ipv6":
                    return "ipv6";
                default:
                    return family;
            }
        }
    }
}
