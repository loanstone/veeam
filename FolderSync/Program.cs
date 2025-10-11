using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace FolderSync
{
    public class FolderSync
    {
        private static int syncPeriod;
        private static string sourceFolder;
        private static string destinationFolder;
        private static string logFilePath;

        public static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();
            ParseArgs(config);
            Sync();
        }

        private static void ParseArgs(IConfiguration config)
        {
            foreach (var kvp in config.AsEnumerable())
            {
                switch (kvp.Key)
                {
                    case "syncPeriod":
                        if (!Int32.TryParse(kvp.Value, out int tempPeriod) && tempPeriod <= 0)
                        {
                            Console.WriteLine($"{kvp.Value} is not a valid integer for sync. Defaulting to 60 minutes.");
                            syncPeriod = 60;
                        }
                        else
                            syncPeriod = tempPeriod;
                        break;
                    case "sourceFolder":
                        if (Directory.Exists(kvp.Value))
                            sourceFolder = kvp.Value;
                        else
                            InvalidPath(kvp.Value);                    
                        break;
                    case "destFolder":
                        if (Directory.Exists(kvp.Value))
                            destinationFolder = kvp.Value;
                        else
                            InvalidPath(kvp.Value);
                        break;
                    case "log":
                        if (Directory.Exists(kvp.Value))
                            logFilePath = Path.Combine(kvp.Value, $"SyncLog_{DateTime.Now.ToString("dd-MM-yyyy HH:mm")}.log");
                        else
                        {
                            Console.WriteLine($"The directory {kvp.Value} does not exist, defaulting to {Path.Combine(Directory.GetCurrentDirectory(), "Log.log")}");
                            logFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"SyncLog_{DateTime.Now.ToString("dd-MM-yyyy HH:mm")}.log");
                        }
                        break;
                    default:
                        Console.WriteLine($"{kvp.Key} is not a supported argument");
                        break;
                }
            }
        }
        private static void InvalidPath(string value)
        {
            Console.WriteLine($"{value} is not a valid directory on your system");
            Environment.Exit(0);
        }

        private static void Sync()
        {
            string[] sourceFiles = RemoveRoot(Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories), ref sourceFolder);
            string[] sourceDirs = RemoveRoot(Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories), ref sourceFolder);
            string[] destFiles = RemoveRoot(Directory.GetFiles(destinationFolder, "*", SearchOption.AllDirectories), ref destinationFolder);
            string[] destDirs = RemoveRoot(Directory.GetDirectories(destinationFolder, "*", SearchOption.AllDirectories), ref destinationFolder);

            
        }
        
        private static string[] RemoveRoot(string[] fsCollection, ref string root)
        {
            for(int i = 0; i < fsCollection.Length; i++)
            {
                fsCollection[i] = fsCollection[i].Replace(root, "");
            }
            return fsCollection;
        }

        // private static bool AreEqual(string[] source, string[] destination)
        // {
        //     if (source.Length != destination.Length)
        //         return false;
            
        // }

        private static string CalculateMD5(string file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }

        private static bool CompareMd5(string checksum1, string checksum2)
        {
            return String.Equals(checksum1, checksum2, StringComparison.OrdinalIgnoreCase);
        }

        enum Actions
        {
            Create,
            Delete,
            Copy,
            Rename,
            Move
        }

        private static void Log(string name, Actions action)
        {
            switch (action)
            {
                case Actions.Create:
                    break;
                case Actions.Delete:
                    break;
                case Actions.Copy:
                    break;
                case Actions.Rename:
                    break;
                case Actions.Move:
                    break;
                default:
                    break;
            }
        }
    }
}