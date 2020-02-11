using System;

namespace Ipset
{
    public class IpsetElementNotFoundException : Exception
    {
        public IpsetElementNotFoundException(string setName, string element) : base($"Element {element} not found in set {setName}")
        { }
    }
}