using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GiroZilla
{
    class Program
    {
        private static void Main(string[] args)
        {
            // Root directory
            var root = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString();

            // Paths
            var nuspecFile = Path.Combine(root, "GiroZilla.nuspec");
            var releasePackages = Path.Combine(root, "ReleasePackages");
            var releaseDir = Path.Combine(root, "Releases");

            // Assembly class to fetch information from the program
            var info = new AssemblyInfo();

            // Custom nuspec file content
            var nuspecText =
                @"<?xml version=""1.0"" encoding=""utf-8""?>" + "\n" +
                @"<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">" + "\n" +
                @"    <metadata>" + "\n" +
                $@"        <id>{info.Title}</id>" + "\n" +
                $@"        <title>{info.Title}</title>" + "\n" +
                 @"        <icon>lib\net45\Assets\Icon\Icon Lines.png</icon>" + "\n" +
                  "        <owners>Minik Gaarde Lambrect, Kristian Kraken Sarka Nielsen</owners>" + "\n" +
                $@"        <version>{info.FileVersion}</version>" + "\n" +
                $@"        <authors>{info.Company}</authors>" + "\n" +
                $@"        <description>{info.Description}</description>" + "\n" +
                $@"        <copyright>{info.Copyright}</copyright>" + "\n" +
                @"    </metadata>" + "\n" +
                @"    <files>" + "\n" +
                @"        <file src=""GiroZilla\obj\Release\**""" + "\n" +
                @"              exclude=""**\*.pdb;**\*.vhost;**\*app.publish\**;**\*TempPE\**;**\*de\**;**\*es\**;**\*fr\**;**\*hu\**;**\*it\**;**\*pt-BR\**;**\*ro\**;**\*ru\**;**\*sv\**;**\*zh-Hans\**;""" + "\n" +
                @"              target=""lib/net45"" />" + "\n" +
                @"    </files>" + "\n" +
                @"</package>";

            // Create the Releases directory if it's not present
            if (!Directory.Exists(releaseDir))
            {
                Directory.CreateDirectory(releaseDir);
            }

            // Create the ReleasePackages directory if it's not present
            if (!Directory.Exists(releasePackages))
            {
                Directory.CreateDirectory(releasePackages);
            }

            // Re-create the nuspec file for each new build
            if (File.Exists(nuspecFile))
            {
                File.Delete(nuspecFile);
            }

            File.WriteAllText(nuspecFile, nuspecText);


            // Delete old files
            var releasePackagesEntries = Directory.GetFiles(releasePackages);
            foreach (var file in releasePackagesEntries)
            {
                File.Delete(file);
            }

            var releaseDirEntries = Directory.GetFiles(releaseDir);
            foreach (var file in releaseDirEntries)
            {
                File.Delete(file);
            }

            // Run commands to pack & releasify the program
            ExecuteCommand($@"nuget pack {root}\GiroZilla.nuspec -OutputDirectory {releasePackages} && squirrel --releasify {releasePackages}\GiroZilla.{info.FileVersion}.nupkg --no-msi --releaseDir={releaseDir}");
        }

        private static void ExecuteCommand(string command)
        {
            try
            {
                var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                var process = Process.Start(processInfo);

                if (process == null)
                {
                    return;
                }

                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => Console.WriteLine(">> " + e.Data);

                process.BeginOutputReadLine();

                process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => Console.WriteLine(">> " + e.Data);

                process.BeginErrorReadLine();

                process.WaitForExit();

                Console.WriteLine("ExitCode: {0}", process.ExitCode);
                process.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.Read();
            }
        }
    }
}
