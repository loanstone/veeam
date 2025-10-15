using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace FolderSync
{
    public class FolderSync
    {
        public static void Main(string[] args)
        {
            Process[] processes = Process.GetProcesses();
            foreach(var process in processes) // Just to make sure only one instance of FolderSync is running
            {
                if(process.ProcessName == "FolderSync")
                {
                    Console.WriteLine("Another instance of FolderSync is already running. No need to run another.");
                    return;
                }
            }

            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();
            Config configuration = ParseArgs(config);
            var syncLogic = new SyncLogic(configuration);
            syncLogic.RunInitialSync();
            syncLogic.RunSyncLoop();
        }

        private static int CheckSyncPeriod(string? period)
        {
            if (!Int32.TryParse(period, out int tempPeriod) || tempPeriod <= 0)
            {
                Console.WriteLine($"{period} is not a valid integer for sync. Defaulting to 60 minutes.");
                return 60;
            }
            return tempPeriod;
        }

        private static string CheckPath(string? path)
        {
            if (Directory.Exists(path))
                return path;
            return null;
        }

        private static string CheckLogFilePath(string? filePath)
        {
            if (Directory.Exists(filePath))
                return filePath;
            else
            {
                Console.WriteLine($"The directory {filePath} does not exist, defaulting to {Path.Combine(Directory.GetCurrentDirectory())}");
                return Path.Combine(Directory.GetCurrentDirectory());
            }
        }

        private static bool IsPathAbsolute(string? path)
        {
            return Path.IsPathRooted(path);
        }

        private static bool ArePathsNested(string source, string dest)
        {
            return source.StartsWith(dest) || dest.StartsWith(source);
        }

        private static void CreateLogFile(string path)
        {
            if (!File.Exists(path))
                File.Create(path).Dispose();
        }
        
        private static bool IsPathInProgramDir(string path)
        {
            return path.StartsWith(Directory.GetCurrentDirectory()) || Directory.GetCurrentDirectory().StartsWith(path);
        }

        private static Config ParseArgs(IConfiguration config) // ParseArgs needs to be redone into something testable. I NEED TO FIGURE OUT HOW TO PREVENT NESTING OF SOURCE AND BAcKUP ROOTS
        {
            Config folderSyncConfig = new Config();
            folderSyncConfig.SyncPeriod = CheckSyncPeriod(config["syncPeriod"]);

            if(string.IsNullOrEmpty(config["sourceFolder"]) || string.IsNullOrEmpty(config["destFolder"]))
                throw new ArgumentException("Source and backup folder arguments are required");

            string? source = IsPathAbsolute(config["sourceFolder"]) ? CheckPath(config["sourceFolder"]) : null;
            string? dest = IsPathAbsolute(config["destFolder"]) ? CheckPath(config["destFolder"]) : null;
            string? log = IsPathAbsolute(config["log"]) ? CheckLogFilePath(config["log"]) : null;

            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(dest))
                throw new ArgumentException("Argument for source or destination folder is either empty or not a valid directory on your system");
                
            source = !ArePathsNested(source, dest) && !IsPathInProgramDir(source) ? config["sourceFolder"] : throw new ArgumentException("The source and backup folders cannot be nested within each other");
            dest = !ArePathsNested(source, dest) && !IsPathInProgramDir(dest) ? config["destFolder"] : throw new ArgumentException("The source and backup folders cannot be nested within each other");

            folderSyncConfig.SourceFolder = source;
            folderSyncConfig.BackupFolder = dest;

            string logFileName = $"SyncLog_{DateTime.Now.ToString("dd-MM-yyyy")}.log";
            if (string.IsNullOrEmpty(log))
            {
                CreateLogFile(Path.Combine(Directory.GetCurrentDirectory(), logFileName));
                folderSyncConfig.LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), logFileName);
            }
            else
            {
                if (!ArePathsNested(source, log) && !ArePathsNested(dest, log))
                {
                    CreateLogFile(Path.Combine(log, logFileName));
                    folderSyncConfig.LogFilePath = log;
                }
                else
                {
                    Console.WriteLine("The log folder cannot be nested within the source or backup folder. Defaulting to program directory.");
                    string currentDir = Directory.GetCurrentDirectory();
                    CreateLogFile(Path.Combine(currentDir, logFileName));
                    folderSyncConfig.LogFilePath = currentDir;
                }
            }

            return folderSyncConfig;

        }
    }
}