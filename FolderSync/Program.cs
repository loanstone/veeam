using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace FolderSync
{
    public class FolderSync
    {
        private static int syncPeriod;
        private static string sourceFolder;
        private static string destinationFolder;
        private static string logFilePath;
        private static string platform;

        public static void Main(string[] args)
        {
            platform = Environment.OSVersion.Platform.ToString();
            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();
            ParseArgs(config);
        }

        private static void ParseArgs(IConfiguration config)
        {
            foreach (var kvp in config.AsEnumerable())
            {
                switch (kvp.Key)
                {
                    case "syncPeriod":
                        if (!Int32.TryParse(kvp.Value, out syncPeriod))
                        {
                            Console.WriteLine($"{kvp.Value} is not a valid integer for sync. Reverting back to default 60 minutes.");
                            syncPeriod = 60;
                        }
                        break;
                    case "sourceFolder":
                        if (DoesDirExist(kvp.Value))
                            sourceFolder = kvp.Value;
                        else
                            InvalidPath(kvp.Value);                    
                        break;
                    case "destFolder":
                        if (DoesDirExist(kvp.Value))
                            destinationFolder = kvp.Value;
                        else
                            InvalidPath(kvp.Value);
                        break;
                    case "log":
                        if (DoesDirExist(Path.GetDirectoryName(kvp.Value)))
                            logFilePath = kvp.Value;
                        else
                        {
                            Console.WriteLine($"The directory {kvp.Value} does not exist, defaulting to {Path.Combine(Directory.GetCurrentDirectory(), "Log.log")}");
                            logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Log.log");
                        }
                        break;
                    default:
                        Console.WriteLine($"{kvp.Key} is not a supported argument");
                        break;
                }
            }
        }

        private static bool DoesDirExist(string argPath)
        {
            return Directory.Exists(argPath);
        }
        
        private static void InvalidPath(string value)
        {
            Console.WriteLine($"{value} is not a valid directory on your system");
            Environment.Exit(0);
        }
    }
}