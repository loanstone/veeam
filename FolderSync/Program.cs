using Microsoft.Extensions.Configuration;
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


        public static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();
            ParseArgs(config);
            InitalSync();
            RunSync();
        }
        
        private static void RunSync()
        {
            while (true)
            {
                CheckForMissingDirs();
                CheckForModifiedFiles();
                CheckForCopiesInSource();
                CheckForMissingFiles();
                CheckForDeletedFiles();
                CheckForDeletedDirs();
                System.Threading.Thread.Sleep(syncPeriod * 60000);
            }
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
            CheckForMissingDirs();
            CheckForDeletedFiles();
            CheckForDeletedDirs();
            CheckForMissingFiles();
        }

        private static void CheckForMissingDirs()
        {
            string[] sourceDirs = Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();
            string[] destDirs = Directory.GetDirectories(backupRoot, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();

            if (sourceDirs == destDirs)
                return;

            foreach (var dir in sourceDirs) // Make sure the folders that are in the source are also in the backup
            {
                string backupDir = dir.Replace(sourceRoot, backupRoot);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                    Log(backupDir, Actions.created);
                }
            }
        }

        private static void CheckForModifiedFiles()
        {
            List<FileProps> sourceFileList = GetAllFilesInDirectory(sourceRoot);
            List<FileProps> backupFileList = GetAllFilesInDirectory(backupRoot);

            foreach (var sourceFile in sourceFileList)
            {
                var backup = backupFileList.Find(backup => backup.IsFileModified(sourceFile));
                if (backup != null)
                {
                    EditFile(sourceFile, backup);
                }
            }
        }

        private static void EditFile(FileProps sourceFile, FileProps backupFile)
        {
            string fullBackupPath = backupFile.GetAbsoluteFilePath;
            File.Delete(backupFile.GetAbsoluteFilePath);
            File.Copy(sourceFile.GetAbsoluteFilePath, fullBackupPath);
            Log(sourceFile.GetFileName, backupFile.GetAbsolutePath, Actions.edited);
        }

        private static void CheckForCopiesInSource()
        {
            List<FileProps> sourceFileList = GetAllFilesInDirectory(sourceRoot);
            List<FileProps> backupFileList = GetAllFilesInDirectory(backupRoot);

            foreach (var sourceFile in sourceFileList)
            {
                var matches = sourceFileList.Where(source => source.IsFileCopied(sourceFile)).ToList();
                if (matches.Count > 0)
                {
                    foreach (var match in matches)
                    {
                        var backup = backupFileList.Find(backup => backup.IsFileTheSame(match));
                        if (backup == null)
                        {
                            CopyFile(match);
                        }
                    }
                }
            }
        }

        private static void CopyFile(FileProps file)
        {
            string newFile = Path.Combine(backupRoot, file.GetRelativeFilePath);
            File.Copy(file.GetAbsoluteFilePath, newFile); // 
            Log(file.GetFileName, newFile, Actions.copied);
        }

        private static void CheckForMissingFiles()
        {
            List<FileProps> sourceFileList = GetAllFilesInDirectory(sourceRoot);
            List<FileProps> backupFileList = GetAllFilesInDirectory(backupRoot);

            foreach (var sourceFile in sourceFileList)
            {
                var backup = backupFileList.Find(backup => backup.IsFileTheSame(sourceFile));
                if (backup == null)
                {
                    CreateFile(sourceFile);
                }
            }
        }
        
        private static void CreateFile(FileProps file)
        {
            string newFile = Path.Combine(backupRoot, file.GetRelativeFilePath);
            File.Copy(file.GetAbsoluteFilePath, newFile); // 
            Log(file.GetFileName, newFile, Actions.created);
        }

        private static void CheckForDeletedFiles()
        {
            List<FileProps> sourceFileList = GetAllFilesInDirectory(sourceRoot);
            List<FileProps> backupFileList = GetAllFilesInDirectory(backupRoot);

            foreach (var backupFile in backupFileList)
            {
                var source = sourceFileList.Find(source => source.IsFileTheSame(backupFile));
                if (source == null)
                {
                    DeleteFile(backupFile);
                }
            }
        }

        private static void DeleteFile(FileProps file)
        {
            File.Delete(file.GetAbsoluteFilePath);
            Log(file.GetFileName, file.GetAbsolutePath, Actions.deleted);
        }

        private static void CheckForDeletedDirs()
        {
            string[] sourceDirs = Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();
            string[] destDirs = Directory.GetDirectories(backupRoot, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();

            for (int i = destDirs.Length - 1; i >= 0; --i)
            {
                string correspondingSourceDir = destDirs[i].Replace(backupRoot, sourceRoot);
                if (!Directory.Exists(correspondingSourceDir))
                {
                    Directory.Delete(destDirs[i], true);
                    Log(destDirs[i], Actions.deleted);
                }
            }
        }

        private static List<FileProps> GetAllFilesInDirectory(string root)
        {
            string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();
            return files.Select(file => new FileProps(file, root)).ToList();
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
            string logTemplate = $"{dt} - {filename} was {action} {grammar} {Path.GetDirectoryName(fullDestinationName)}";
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