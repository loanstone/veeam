using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace FolderSync
{
    public class FolderSync
    {
        private static int syncPeriod = 60; // Set default sync rate to 60 minutes
        private static string sourceFolder;
        private static string destinationFolder;
        private static string logFilePath = Directory.GetCurrentDirectory(); // Set default log file path to the current dir
        private static System.PlatformID platform;

        public static void Main(string[] args)
        {
            platform = Environment.OSVersion.Platform;
            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();
            ParseArgs(config);
        }

        private static void ParseArgs(IConfiguration config)
        {
            Dictionary<string, bool> isSet;
            foreach (var kvp in config.AsEnumerable())
            {
                switch (kvp.Key)
                {
                    case "syncPeriod":
                        if (!Int32.TryParse(kvp.Value, out syncPeriod))
                            Console.WriteLine($"{kvp.Value} is not a valid integer for sync. Reverting back to default 60 minutes.");
                        break;
                    case "sourceFolder":
                        sourceFolder = EnsureOsDirectory(kvp.Value);
                        break;
                    case "destFolder":
                        destinationFolder = EnsureOsDirectory(kvp.Value);
                        break;
                    case "log":
                        logFilePath = EnsureOsDirectory(kvp.Value);
                        break;
                    default:
                        Console.WriteLine($"{kvp.Key} is not a supported argument");
                        break;
                }
            }
        }
        
        private static string EnsureOsDirectory(string argPath)
        {
            return argPath;
        }
    }
}