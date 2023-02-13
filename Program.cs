using System;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Threading;
using System.Net;
using System.Text;
using System.Xml.Linq;
using static JavaCommons.Extra.Extra;

namespace Run;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Console.Error.WriteLine(args.Length);
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Please specify program name.");
            Environment.Exit(1);
        }
        string appName = args[0];
        string userName = "nuget-tools";
        string[] appNameParts = appName.Split('.');
        if (appNameParts.Length == 1)
        {
        }
        else if (appNameParts.Length == 2)
        {
            userName = appNameParts[0];
        }
        else
        {
            Console.Error.WriteLine("Invalid program name.");
            Environment.Exit(1);
        }
        ArraySegment<string> arySeg = new ArraySegment<string>(args, 1, args.Length - 1);
        string[] argsSlice = arySeg.ToArray();
        string xmlUrl = $"https://github.com/{userName}/tools/releases/download/windows-64bit/{appName}.xml";
        RunSelectedProgram(appName, xmlUrl, argsSlice);
    }

    static void RunSelectedProgram(string appName, string xmlUrl, string[] args)
    {
        Console.Error.WriteLine(appName);
        Console.Error.WriteLine(xmlUrl);
        var xml = GetStringFromUrl(xmlUrl);
        XDocument doc = XDocument.Parse(xml);
        XElement root = doc.Root;
        var version = root.Element("version").Value;
        var url = root.Element("url").Value;
        //var mainDll = $"{appName}.exe";
        //var mainClass = $"{appName}.exe";
        Console.Error.WriteLine(version);
        Console.Error.WriteLine(url);
        var profilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Console.Error.WriteLine(profilePath);
        var installPath = $"{profilePath}/.nuget-tools/.tools/{appName}-{version}";
        Console.Error.WriteLine(installPath);
        if (!Directory.Exists(installPath))
        {
            Console.Error.WriteLine($"{installPath} doest not exist.");
            DirectoryInfo di = new DirectoryInfo(installPath);
            DirectoryInfo diParent = di.Parent;
            string parent = diParent.FullName;
            Console.Error.WriteLine($"{parent} is begin prepared.");
            Directory.CreateDirectory(parent);
            string destinationPath = $"{parent}/{appName}-{version}.zip";
            FileInfo fi = new FileInfo(destinationPath);
            if (!fi.Exists)
            {
                Console.Error.WriteLine($"{destinationPath} is begin downloaded.");
                DownloadBinaryFromUrl(url, destinationPath);
                Console.Error.WriteLine($"{destinationPath} has been downloaded.");
            }

            Console.Error.WriteLine($"{installPath}: installing...");
            ZipFile.ExtractToDirectory(destinationPath, installPath);
            Console.Error.WriteLine($"{installPath}: installed.");
        }

        Console.Error.WriteLine($"{appName} is being run.");
        //Thread.Sleep(1000);
        StartAssembly(installPath, appName, version, args);
    }

    static void StartAssembly(string installPath, string appName, string version, string[] args)
    {
        string argList = "";
        for (int i = 0; i < args.Length; i++)
        {
            if (i > 0) argList += " ";
            argList += $"\"{args[i]}\"";
        }
        Process process = new Process();
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        if (File.Exists($"{installPath}/{appName}.exe"))
        {
            process.StartInfo.FileName = $"{installPath}/{appName}.exe";
            process.StartInfo.Arguments = argList;
        }
        else
        {
            process.StartInfo.FileName = "dotnet";;
            process.StartInfo.Arguments = $"\"{installPath}/{appName}.dll\" {argList}";
        }
        process.OutputDataReceived += (sender, e) => { Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (sender, e) => { Console.Error.WriteLine(e.Data); };
        process.Start();
        Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
        {
            process.Kill();
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        process.CancelOutputRead();
        process.CancelErrorRead();
        Environment.Exit(process.ExitCode);
    }
}
