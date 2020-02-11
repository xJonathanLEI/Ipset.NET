using System;

namespace Ipset
{
    public class IpsetSetNotFoundException : Exception
    {
        public IpsetSetNotFoundException(string setName) : base($"Ipset set {setName} does not exist")
        { }
    }
}