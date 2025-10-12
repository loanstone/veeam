using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace FolderSync
{
    public class FolderSync
    {
        [NotNull]
        private static int syncPeriod;
        [NotNull]
        private static string sourceRoot;
        [NotNull]
        private static string backupRoot;
        [NotNull]
        private static string logFilePath;
        private static List<FileProps> sourceFileList;
        private static List<FileProps> backupFileList;

        public static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();
            ParseArgs(config);
            InitalSync();
            // Sync();
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
                            sourceRoot = kvp.Value;
                        else
                            InvalidPath(kvp.Value);                    
                        break;
                    case "destFolder":
                        if (Directory.Exists(kvp.Value))
                            backupRoot = kvp.Value;
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

        // Here we make sure that all required directories exist and clean the backup folder if the source is empty. We're not logging deletion here because it's not really a part of the actual file sync.
        private static void InitalSync()
        {
            string[] sourceDirs = Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();
            string[] destDirs = Directory.GetDirectories(backupRoot, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();

            foreach (var dir in sourceDirs) // Make sure the folders that are in the source are also in the backup
            {
                string backupDir = dir.Replace(sourceRoot, backupRoot);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                    Log(backupDir, Actions.created);
                }
            }

            if(Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories).Length == 0 && Directory.GetFiles(backupRoot, "*", SearchOption.AllDirectories).Length > 0)
            {
                foreach (var file in Directory.GetFiles(backupRoot, "*", SearchOption.AllDirectories))
                    File.Delete(file);
            }

            if (sourceDirs.Length == 0 && destDirs.Length > 0) // If the source folder is empty, make sure the backup folder is empty as well
            {
                for (int i = destDirs.Length - 1; i >= 0; --i)
                    Directory.Delete(destDirs[i]);
            }
        }

        private static void Sync()
        {
            sourceFileList = new List<FileProps>();
            backupFileList = new List<FileProps>();
            string[] sourceFiles = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);
            foreach (string file in sourceFiles)
                sourceFileList.Add(new FileProps(file, sourceRoot));

            string[] destFiles = Directory.GetFiles(backupRoot, "*", SearchOption.AllDirectories);
            foreach (string file in destFiles)
                backupFileList.Add(new FileProps(file, backupRoot));

            // if (sourceFileList.Count == 0 && backupFileList.Count > 0) // If the source folder is empty, delete all files in the backup folder
            // {
            //     foreach (var file in backupFileList)
            //     {
            //         File.Delete(file.GetAbsoluteFilePath);
            //         Log(file.GetFileName, file.GetAbsolutePath, Actions.deleted);
            //     }
            // }

            foreach (var file in sourceFileList)
            {
                var matchingSourceFiles = sourceFileList.FindAll(source => source.IsFileCopied(file.GetFileName, file.GetMD5Code, file.GetRelativePath));
                if (matchingSourceFiles.Count > 0) // An exact copy of the file exists in a different directory
                {
                    foreach (var matchingSourceFile in matchingSourceFiles)
                    {
                        var match = backupFileList.Find(backup => backup.IsFileTheSame(file.GetRelativeFilePath, file.GetMD5Code)); // See if the copy is already backed up
                        if (match == null)
                        {
                            CreateCopyFile(file, Actions.copied);
                            continue;
                        }
                    }
                }
                var matchingBackupFile = backupFileList.Find(backup => backup.IsFileTheSame(file.GetRelativeFilePath, file.GetMD5Code));
                if (matchingBackupFile == null) // if no match of the file is found, create a backup
                {
                    CreateCopyFile(file, Actions.created);
                    continue;
                }
            }

            foreach (var file in backupFileList)
            {
                var matchingSourceFile = sourceFileList.Find(source => source.IsFileTheSame(file.GetRelativeFilePath, file.GetMD5Code));
                if (matchingSourceFile == null) // the file was deleted
                {
                    File.Delete(file.GetAbsoluteFilePath);
                    Log(file.GetFileName, file.GetAbsolutePath, Actions.deleted);
                }
            }

        }

        private static void CreateCopyFile(FileProps file, Actions action)
        {
            string newFile = Path.Combine(backupRoot, file.GetRelativeFilePath);
            File.Copy(file.GetAbsoluteFilePath, newFile); // 
            Log(file.GetFileName, newFile, action);
        }

        private static void EditFile(FileProps file, Actions action)
        {
            
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

        private static void Log(string dir, Actions action)
        {
            string dt = DateTime.Now.ToString("dd-MM-yyyy HH:mm");
            string logTemplate = $"{dt} - Directory {dir} was {action}";
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