using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.EventStream;

namespace ME3TweaksCore.Helpers
{
    /// <summary>
    /// Class that can determine if a version of .NET Core is installed
    /// </summary>
    public class DotNetRuntimeVersionDetector
    {
        /// <summary>
        /// This is very windows specific
        /// </summary>
        /// <param name="desktopVersionsOnly">If it needs to filter to Windows Desktop versions only (WPF/Winforms).</param>
        /// <returns>List of versions matching the specified version</returns>
        public static async Task<Version[]> GetInstalledRuntimeVersions(bool desktopVersion)
        {
            // No validation. Make sure exit code is checked in the calling process.
            var cmd = Cli.Wrap(@"dotnet.exe").WithArguments(@"--list-runtimes").WithValidation(CommandResultValidation.None);
            var runtimes = new List<Version>();
            await foreach (var cmdEvent in cmd.ListenAsync())
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent started:
                        break;
                    case StandardOutputCommandEvent stdOut:
                        if (string.IsNullOrWhiteSpace(stdOut.Text))
                        {
                            continue;
                        }

                        if (stdOut.Text.StartsWith(@"Microsoft.NETCore.App") && !desktopVersion)
                        {
                            runtimes.Add(parseVersion(stdOut.Text));
                        }
                        else if (stdOut.Text.StartsWith(@"Microsoft.WindowsDesktop.App") && desktopVersion)
                        {
                            runtimes.Add(parseVersion(stdOut.Text));
                        }
                        break;
                    case StandardErrorCommandEvent stdErr:
                        break;
                    case ExitedCommandEvent exited:
                        break;
                }
            }

            return runtimes.ToArray();
        }

        private static Version parseVersion(string stdOutText)
        {
            var split = stdOutText.Split(' ');
            return Version.Parse(split[1]); // 0 = SDK name, 1 = version, 2+ = path parts
        }
    }
}
