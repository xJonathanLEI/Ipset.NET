using System.Collections.Generic;

namespace Ipset
{
    public class IpsetSet
    {
        public string Name { get; internal set; }
        public IpsetSetType Type { get; internal set; }
        public int Revision { get; internal set; }
        public string Header { get; internal set; }
        public int SizeInMemory { get; internal set; }
        public int References { get; internal set; }
        public IReadOnlyCollection<string> Members { get; internal set; }
    }
}