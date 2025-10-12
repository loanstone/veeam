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

            string[] sourceDirs = Directory.GetDirectories(sourceFolder).OrderBy(s=>s).ToArray();
            string[] destDirs = Directory.GetDirectories(destinationFolder).OrderBy(s => s).ToArray();
            
            if(sourceDirs != destDirs) // Make sure same directories exist. at the end of Sync() we're making sure to recursively delete directories that no longer exist in source.
            {
                
            }

            string[] sourceFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
            foreach (string file in sourceFiles)
                sourceFileList.Add(new FileProps(file, sourceFolder));

            string[] destFiles = Directory.GetFiles(destinationFolder, "*", SearchOption.AllDirectories);
            foreach (string file in destFiles)
                backupFileList.Add(new FileProps(file, destinationFolder));

            if(sourceFileList.Count == 0 && backupFileList.Count > 0) // If the source folder is empty, delete all files in the backup folder
            {
                foreach (var file in backupFileList) 
                {
                    File.Delete(file.AbsoluteFilePath);
                    Log(file.FileName, file.AbsolutePath, Actions.deleted);
                }
            }

            foreach (var file in sourceFileList)
            {
                var matchingBackupByFile = backupFileList.FindAll(backup => backup.RelativeFilePath == file.RelativeFilePath);
                var matchingBackupByMD5 = backupFileList.FindAll(backup => backup.md5Code == file.md5Code);

                if (matchingBackupByFile.Count == 1 && matchingBackupByMD5.Count == 1) // File should be the same here, DOESN'T WORK IF FILE HAS A DIFFERENT NAME BUT THE SAME HASH
                {
                    var backupFile = matchingBackupByFile.First(a => a.RelativePath == file.RelativePath);
                    if (backupFile.RelativeFilePath == file.RelativeFilePath && backupFile.md5Code == file.md5Code)
                        continue;
                }
                else if (matchingBackupByFile.Count == 0 && matchingBackupByMD5.Count == 0) // File is not backed up, create a copy
                {
                    string destinationDir = Path.Combine(destinationFolder, file.RelativePath);
                    string finalFile = Path.Combine(destinationDir, file.FileName);
                    if (!Directory.Exists(destinationDir))
                        Directory.CreateDirectory(destinationDir);
                    File.Copy(file.AbsoluteFilePath, finalFile);
                    Log(file.FileName, destinationDir, Actions.created);
                }
            }

            foreach(var file in backupFileList)
            {
                var matchingSourceByFile = sourceFileList.FindAll(source => source.RelativeFilePath == file.RelativeFilePath);
                var matchingSourceByMD5 = sourceFileList.FindAll(source => source.md5Code == file.md5Code);

                if (matchingSourceByFile.Count == 0 && matchingSourceByMD5.Count == 0) // the file was deleted
                {
                    File.Delete(file.AbsoluteFilePath);
                    Log(file.FileName, file.AbsoluteFilePath, Actions.deleted);
                }
                else if (matchingSourceByMD5.Count > 1) // File was copied, maybe even renamed, but we're not checking for that - DOESN'T WORK
                {
                    foreach(var sourceFile in matchingSourceByMD5)
                    {
                        string destinationDir = Path.Combine(destinationFolder, sourceFile.RelativePath);
                        string finalFile = Path.Combine(destinationDir, file.FileName);
                        if (!Directory.Exists(destinationDir))
                            Directory.CreateDirectory(destinationDir);
                        if (!File.Exists(finalFile))
                        {
                            File.Copy(sourceFile.AbsoluteFilePath, finalFile);
                            Log(sourceFile.FileName, destinationDir, Actions.copied);
                        }
                    }
                }
            }
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