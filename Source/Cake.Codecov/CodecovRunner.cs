using System;
using System.Collections.Generic;

using Cake.Codecov.Internals;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Codecov
{
    internal sealed class CodecovRunner : Tool<CodecovSettings>
    {
        private readonly IPlatformDetector platformDetector;

        internal CodecovRunner(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : this(new Internals.PlatformDetector(), fileSystem, environment, processRunner, tools)
        {
        }

        internal CodecovRunner(Internals.IPlatformDetector platformDetector, IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
            : base(fileSystem, environment, processRunner, tools)
        {
            this.platformDetector = platformDetector ?? throw new ArgumentNullException(nameof(platformDetector));
        }

        internal void Run(CodecovSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Run(settings, GetArguments(settings));
        }

        protected override IEnumerable<string> GetToolExecutableNames()
        {
            if (platformDetector.IsLinuxPlatform())
            {
                yield return "linux-x64/codecov";
                yield return "codecov";
            }
            else if (platformDetector.IsOsxPlatform())
            {
                yield return "osx-x64/codecov";
                yield return "codecov";
            }
            else
            {
                // Just to make sonarlint happy :)
            }

            yield return "codecov.exe";
        }

        protected override string GetToolName() => "Codecov";

        private static void AddValue(ProcessArgumentBuilder builder, string key, IEnumerable<string> value)
        {
            string joinedValue = string.Join(" ", value);
            AddValue(builder, key, joinedValue);
        }

        private static void AddValue(ProcessArgumentBuilder builder, string key, Uri value)
        {
            if (value.IsWellFormedOriginalString())
            {
                AddValue(builder, key, value.ToString());
            }
        }

        private static void AddValue(ProcessArgumentBuilder builder, KeyValuePair<string, object> argument, bool value)
        {
            if (value)
            {
                builder.Append(argument.Key);
            }
        }

        private static void AddValue(ProcessArgumentBuilder builder, string key, string value)
        {
            builder.AppendSwitchQuoted(key, " ", value);
        }

        private static ProcessArgumentBuilder GetArguments(CodecovSettings settings)
        {
            var builder = new ProcessArgumentBuilder();

            foreach (var argument in settings.GetAllArguments())
            {
                switch (argument.Value)
                {
                    case bool value:
                        AddValue(builder, argument, value);
                        break;

                    case Uri value:
                        AddValue(builder, argument.Key, value);
                        break;

                    case IEnumerable<string> value:
                        AddValue(builder, argument.Key, value);
                        break;

                    default:
                        AddValue(builder, argument.Key, argument.Value.ToString());
                        break;
                }
            }

            return builder;
        }
    }
}
