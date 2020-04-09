using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

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
            using var outputs = GetOutputFromCommand($"list {setName}");

            // Errors reported?
            using (var sr = new StreamReader(outputs.StandardError))
            {
                if (!sr.EndOfStream)
                {
                    string errorLine = sr.ReadLine();

                    if (errorLine.Contains(SetDoesNotExistText))
                        throw new IpsetSetNotFoundException(setName);

                    throw new NotImplementedException($"Uknown error message: {errorLine}");
                }
            }

            // Reads standard output
            using (var sr = new StreamReader(outputs.StandardOutput))
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
            using var outputs = GetOutputFromCommand($"add {setName} {member}");

            // Errors reported?
            using (var sr = new StreamReader(outputs.StandardError))
            {
                if (!sr.EndOfStream)
                {
                    string errorLine = sr.ReadLine();

                    if (errorLine.Contains(SetDoesNotExistText))
                        throw new IpsetSetNotFoundException(setName);

                    if (errorLine.Contains(ElementAlreadyAddedText))
                        throw new IpsetElementAlreadyInSetException(setName, member);

                    if (errorLine.Contains(ResolveIpv4AddressFailedText))
                        throw new IpsetInvalidIpAddressException(member);

                    throw new NotImplementedException($"Uknown error message: {errorLine}");
                }
            }
        }

        public static void RemoveMemberFromSet(string setName, string member)
        {
            using var outputs = GetOutputFromCommand($"del {setName} {member}");

            // Errors reported?
            using (var sr = new StreamReader(outputs.StandardError))
            {
                if (!sr.EndOfStream)
                {
                    string errorLine = sr.ReadLine();

                    if (errorLine.Contains(SetDoesNotExistText))
                        throw new IpsetSetNotFoundException(setName);

                    if (errorLine.Contains(ElementNotAddedText))
                        throw new IpsetElementNotFoundException(setName, member);

                    if (errorLine.Contains(ResolveIpv4AddressFailedText))
                        throw new IpsetInvalidIpAddressException(member);

                    throw new NotImplementedException($"Uknown error message: {errorLine}");
                }
            }
        }

        private static ProcessOutputs GetOutputFromCommand(string arguments)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("ipset", arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
            };

            var standardStream = new MemoryStream();
            var errorStream = new MemoryStream();

            var standardWriter = new StreamWriter(standardStream);
            var errorWriter = new StreamWriter(errorStream);

            using EventWaitHandle outputWaitHandle = new AutoResetEvent(false);
            using EventWaitHandle errorWaitHandle = new AutoResetEvent(false);

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data is null)
                {
                    outputWaitHandle.Set();
                }
                else
                {
                    standardWriter.WriteLine(e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data is null)
                {
                    errorWaitHandle.Set();
                }
                else
                {
                    errorWriter.WriteLine(e.Data);
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            outputWaitHandle.WaitOne();
            errorWaitHandle.WaitOne();

            standardWriter.Flush();
            errorWriter.Flush();

            standardStream.Seek(0, SeekOrigin.Begin);
            standardStream.Seek(0, SeekOrigin.Begin);

            return new ProcessOutputs(
                standardStream,
                errorStream
            );
        }

        private static IpsetSetType ParseSetType(string value)
        {
            if (setTypesByName.TryGetValue(value, out IpsetSetType type))
                return type;

            throw new NotImplementedException($"Unknown ipset set type: {value}");
        }

        private class ProcessOutputs : IDisposable
        {
            public Stream StandardOutput { get; }
            public Stream StandardError { get; }

            public ProcessOutputs(Stream standardOutput, Stream standardError)
            {
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            public void Dispose()
            {
                StandardOutput.Dispose();
                StandardError.Dispose();
            }
        }
    }
}