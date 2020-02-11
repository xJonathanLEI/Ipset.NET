using System;

namespace Ipset
{
    public class IpsetElementAlreadyInSetException : Exception
    {
        public IpsetElementAlreadyInSetException(string setName, string element) : base($"Element {element} already exists in set {setName}")
        { }
    }
}