using System;
using System.IO;
using Microsoft.Extensions.Configuration;

using System.Runtime.InteropServices;

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
                            logFilePath = Path.Combine(kvp.Value, $"SyncLog_{DateTime.Now.ToString("dd-MM-yyyy")}.log");
                        else
                        {
                            Console.WriteLine($"The directory {kvp.Value} does not exist, defaulting to {Path.Combine(Directory.GetCurrentDirectory(), "Log.log")}");
                            logFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"SyncLog_{DateTime.Now.ToString("dd-MM-yyyy")}.log");
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
            List<FileProps> sourceFileList = new List<FileProps>();
            List<FileProps> backupFileList = new List<FileProps>();
            FileProps sourceProps;
            FileProps backupProps;
            string[] sourceFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
            foreach (string file in sourceFiles)
            {
                sourceProps = new FileProps(file, sourceFolder);
                sourceFileList.Add(sourceProps);
            }

            string[] destFiles = Directory.GetFiles(destinationFolder, "*", SearchOption.AllDirectories);
            foreach (string file in destFiles)
            {
                backupProps = new FileProps(file, destinationFolder);
                backupFileList.Add(backupProps);
            }

            foreach(var file in sourceFileList)
            {
                var matchingBackupByFile = backupFileList.FindAll(backup => backup.RelativeFilePath == file.RelativeFilePath); // the problem here is if there are multiple occurrences of the same file
                var matchingBackupByMD5 = backupFileList.FindAll(backup => backup.md5Code == file.md5Code); // the problem here is if there are multiple occurrences of the same md5

                // if (backupDict.ContainsKey(kvp.Key) && IsSameMD5(backupDict[kvp.Key], sourceDict[kvp.Key])) // nothing to do here, both files are the same
                //     continue;
                // else if (!backupDict.ContainsKey(kvp.Key) && !backupDict.ContainsValue(kvp.Value)) // the original file has no backup, it will be created here
                // {
                //     string newFile = Path.Combine(destinationFolder, kvp.Key);
                //     string newDirectory = Path.GetDirectoryName(newFile);
                //     if (!Directory.Exists(newDirectory))
                //         Directory.CreateDirectory(newDirectory);
                //     File.Copy(Path.Combine(sourceFolder, kvp.Key), newFile);
                //     Log(Path.GetFileName(newFile), newDirectory, Actions.created);
                // }
                // else if (backupDict.ContainsKey(kvp.Key) && !IsSameMD5(backupDict[kvp.Key], sourceDict[kvp.Key])) // the original file was modified
                // {
                //     string file = Path.Combine(destinationFolder, kvp.Key);
                //     File.Delete(file);
                //     File.Copy(Path.Combine(sourceFolder, kvp.Key), file);
                //     Log(Path.GetFileName(file), Path.GetDirectoryName(file), Actions.edited);
                // }
                // else if (!backupDict.ContainsKey(kvp.Key) && backupDict.ContainsValue(kvp.Value)) // the original file was moved or renamed
                // {
                //     // check if it was renamed
                //     string originalPath = RemoveRoot(Path.GetDirectoryName(Path.Combine(sourceFolder, kvp.Key)), ref sourceFolder); // why am i not getting back the stripped path?

                //     // check if it was moved
                // }
            }
        }
        
        private static string[] RemoveRoot(string[] fsCollection, ref string root)
        {
            for(int i = 0; i < fsCollection.Length; i++)
            {
                fsCollection[i] = fsCollection[i].Replace(root, "");
            }
            return fsCollection.OrderBy(s=>s).ToArray();
        }

        private static string RemoveRoot(string path, ref string root)
        {
            path = path.Replace(root, "");
            return path;
        }

        private static bool IsSameMD5(string origin, string backup)
        {
            return String.Equals(origin, backup, StringComparison.OrdinalIgnoreCase);
        }

        enum Actions
        {
            created,
            deleted,
            copied,
            renamed,
            moved,
            edited
        }

        private static void Log(string filename, string fullDestinationName, Actions action)
        {
            string dt = DateTime.Now.ToString("dd-MM-yyyy HH:mm");
            string grammar = "";
            switch (action)
            {
                case Actions.created:
                case Actions.edited:
                    grammar = "in";
                    break;
                case Actions.deleted:
                    grammar = "from";
                    break;
                case Actions.copied:
                case Actions.renamed:
                case Actions.moved:
                    grammar = "to";
                    break;
                default:
                    break;
            }
            string logTemplate = $"{dt} - {filename} was {action} {grammar} {fullDestinationName}";
            Console.WriteLine(logTemplate);

            if (!Directory.Exists(Path.GetDirectoryName(logFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            if (!File.Exists(logFilePath))
                File.Create(logFilePath).Dispose();

            using (var logFile = File.AppendText(logFilePath))
            {
                logFile.WriteLine(logTemplate);
            }
            
        }
    }
}