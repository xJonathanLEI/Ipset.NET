using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Ipset
{
    public static class Ipset
    {
        private const string SetDoesNotExistText = "The set with the given name does not exist";
        private const string ElementAlreadyAddedText = "Element cannot be added to the set: it's already added";
        private const string ElementNotAddedText = "Element cannot be deleted from the set: it's not added";
        private const string ResolveIpv4AddressFailedText = "resolving to IPv4 address failed";

        private static readonly Dictionary<string, IpsetSetType> setTypesByName = new Dictionary<string, IpsetSetType>(StringComparer.OrdinalIgnoreCase)
        {
            { "hash:ip", IpsetSetType.HashIp },
        };

        public static IpsetSet GetSetByName(string setName)
        {
            var outputs = GetOutputFromCommand($"list {setName}");

            // Errors reported?
            if (!outputs.StandardError.EndOfStream)
            {
                string errorLine = outputs.StandardError.ReadLine();

                if (errorLine.Contains(SetDoesNotExistText))
                    throw new IpsetSetNotFoundException(setName);

                throw new NotImplementedException($"Uknown error message: {errorLine}");
            }

            using (var sr = outputs.StandardOutput)
            {
                string firstLine = sr.ReadLine();

                if (firstLine.Contains(SetDoesNotExistText))
                    throw new IpsetSetNotFoundException(setName);

                var set = new IpsetSet();

                ParseLine(set, firstLine);

                while (!sr.EndOfStream)
                {
                    string currentLine = sr.ReadLine();

                    if (currentLine.Equals("Members:"))
                    {
                        var members = new HashSet<string>();

                        while (!sr.EndOfStream)
                        {
                            string currentMember = sr.ReadLine();
                            members.Add(currentMember);
                        }

                        set.Members = members;
                    }
                    else
                    {
                        ParseLine(set, currentLine);
                    }
                }

                return set;
            }

            void ParseLine(IpsetSet set, string line)
            {
                if (line.StartsWith("Name: "))
                {
                    set.Name = line.Substring("Name: ".Length);
                }
                else if (line.StartsWith("Type: "))
                {
                    set.Type = ParseSetType(line.Substring("Type: ".Length));
                }
                else if (line.StartsWith("Revision: "))
                {
                    set.Revision = int.Parse(line.Substring("Revision: ".Length));
                }
                else if (line.StartsWith("Header: "))
                {
                    set.Header = line.Substring("Header: ".Length);
                }
                else if (line.StartsWith("Size in memory: "))
                {
                    set.SizeInMemory = int.Parse(line.Substring("Size in memory: ".Length));
                }
                else if (line.StartsWith("References: "))
                {
                    set.References = int.Parse(line.Substring("References: ".Length));
                }
            }

            throw new Exception();
        }

        public static void AddMemberToSet(string setName, string member)
        {
            var outputs = GetOutputFromCommand($"add {setName} {member}");

            // Errors reported?
            if (!outputs.StandardError.EndOfStream)
            {
                string errorLine = outputs.StandardError.ReadLine();

                if (errorLine.Contains(SetDoesNotExistText))
                    throw new IpsetSetNotFoundException(setName);

                if (errorLine.Contains(ElementAlreadyAddedText))
                    throw new IpsetElementAlreadyInSetException(setName, member);

                if (errorLine.Contains(ResolveIpv4AddressFailedText))
                    throw new IpsetInvalidIpAddressException(member);

                throw new NotImplementedException($"Uknown error message: {errorLine}");
            }
        }

        public static void RemoveMemberFromSet(string setName, string member)
        {
            var outputs = GetOutputFromCommand($"del {setName} {member}");

            // Errors reported?
            if (!outputs.StandardError.EndOfStream)
            {
                string errorLine = outputs.StandardError.ReadLine();

                if (errorLine.Contains(SetDoesNotExistText))
                    throw new IpsetSetNotFoundException(setName);

                if (errorLine.Contains(ElementNotAddedText))
                    throw new IpsetElementNotFoundException(setName, member);

                if (errorLine.Contains(ResolveIpv4AddressFailedText))
                    throw new IpsetInvalidIpAddressException(member);

                throw new NotImplementedException($"Uknown error message: {errorLine}");
            }
        }

        private static ProcessOutputs GetOutputFromCommand(string arguments)
        {
            var startInfo = new ProcessStartInfo("ipset", arguments);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = Process.Start(startInfo);
            process.WaitForExit();

            return new ProcessOutputs(
                process.StandardOutput,
                process.StandardError
            );
        }

        private static IpsetSetType ParseSetType(string value)
        {
            if (setTypesByName.TryGetValue(value, out IpsetSetType type))
                return type;

            throw new NotImplementedException($"Unknown ipset set type: {value}");
        }

        private class ProcessOutputs
        {
            public StreamReader StandardOutput { get; }
            public StreamReader StandardError { get; }

            public ProcessOutputs(StreamReader standardOutput, StreamReader standardError)
            {
                StandardOutput = standardOutput;
                StandardError = standardError;
            }
        }
    }
}