using System;

namespace Ipset
{
    public class IpsetInvalidIpAddressException : Exception
    {
        public IpsetInvalidIpAddressException(string ipAddress) : base($"Invalid IP address {ipAddress}")
        { }
    }
}