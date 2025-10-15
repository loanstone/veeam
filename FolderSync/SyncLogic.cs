namespace FolderSync
{
    public class SyncLogic
    {
        private List<string> _logs;
        public Config Config { get; }
        public SyncLogic(Config config)
        {
            Config = config;
            _logs = new List<string>();
        }

        public void RunInitialSync()
        {
            CheckExistingDirs();
            CheckForMissingDirs();
            CheckForDeletedFiles();
            CheckForDeletedDirs();
            CheckForMissingFiles();
            WriteLogs();
        }

        public void RunSyncLoop()
        {
            while (true)
            {
                CheckExistingDirs();
                CheckForMissingDirs();
                CheckForModifiedFiles();
                CheckForCopiesInSource();
                CheckForMissingFiles();
                CheckForDeletedFiles();
                CheckForDeletedDirs();
                WriteLogs();
                Thread.Sleep(Config.SyncPeriod * 60000);
            }
        }
        
        private void CheckExistingDirs()
        {
            if (!Directory.Exists(Config.SourceFolder))
                throw new DirectoryNotFoundException($"Source folder '{Config.SourceFolder}' no longer exists");
            if (!Directory.Exists(Config.BackupFolder))
                throw new DirectoryNotFoundException($"Backup folder '{Config.BackupFolder}' no longer exists");
            if (!Directory.Exists(Directory.GetParent(Config.LogFilePath).ToString()))
                throw new DirectoryNotFoundException($"Log file path '{Config.LogFilePath}' no longer exists");
            if (!File.Exists(Config.LogFilePath))
                throw new FileNotFoundException($"Log file '{Config.LogFilePath}' no longer exists");
        }

        private void WriteLogs()
        {
            using (var logFile = File.AppendText(Config.LogFilePath))
            {
                foreach (var line in _logs)
                {
                    logFile.WriteLine(line);
                    Console.WriteLine(line);
                }
            }
            _logs.Clear();
        }

        private void CheckForMissingDirs()
        {
            string[] sourceDirs = Directory.GetDirectories(Config.SourceFolder, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();
            string[] destDirs = Directory.GetDirectories(Config.BackupFolder, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();

            string[] sourceDirsRelative = sourceDirs.Select(sourceFolder => sourceFolder.Replace(Config.SourceFolder, "")).ToArray();
            string[] destDirsRelative = destDirs.Select(backupFolder => backupFolder.Replace(Config.BackupFolder, "")).ToArray();

            if (sourceDirsRelative.SequenceEqual(destDirsRelative))
                return;

            foreach (var dir in sourceDirs)
            {
                string backupDir = dir.Replace(Config.SourceFolder, Config.BackupFolder);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                    _logs.Add(Log(backupDir, Actions.created));
                }
            }
        }

        private void CheckForModifiedFiles()
        {
            var sourceFileList = GetAllFilesInDirectory(Config.SourceFolder);
            var backupFileList = GetAllFilesInDirectory(Config.BackupFolder);

            foreach (var sourceFile in sourceFileList)
            {
                var backup = backupFileList.Find(b => b.IsFileModified(sourceFile));
                if (backup != null)
                {
                    EditFile(sourceFile, backup);
                }
            }
        }

        private void EditFile(FileProps sourceFile, FileProps backupFile)
        {
            string fullBackupPath = backupFile.GetAbsoluteFilePath;
            File.Delete(backupFile.GetAbsoluteFilePath);
            File.Copy(sourceFile.GetAbsoluteFilePath, fullBackupPath);
            _logs.Add(Log(sourceFile.GetFileName, backupFile.GetAbsolutePath, Actions.edited));
        }

        private void CheckForCopiesInSource()
        {
            var sourceFileList = GetAllFilesInDirectory(Config.SourceFolder);
            var backupFileList = GetAllFilesInDirectory(Config.BackupFolder);

            foreach (var sourceFile in sourceFileList)
            {
                var matches = sourceFileList.Where(s => s.IsFileCopied(sourceFile)).ToList();
                if (matches.Count > 0)
                {
                    foreach (var match in matches)
                    {
                        var backup = backupFileList.Find(b => b.IsFileTheSame(match));
                        if (backup == null)
                        {
                            CopyFile(match);
                        }
                    }
                }
            }
        }

        private void CopyFile(FileProps file)
        {
            string newFile = Path.Combine(Config.BackupFolder, file.GetRelativeFilePath);
            File.Copy(file.GetAbsoluteFilePath, newFile);
            _logs.Add(Log(file.GetFileName, newFile, Actions.copied));
        }

        private void CheckForMissingFiles()
        {
            var sourceFileList = GetAllFilesInDirectory(Config.SourceFolder);
            var backupFileList = GetAllFilesInDirectory(Config.BackupFolder);

            foreach (var sourceFile in sourceFileList)
            {
                var backup = backupFileList.Find(b => b.IsFileTheSame(sourceFile));
                if (backup == null)
                {
                    CreateFile(sourceFile);
                }
            }
        }

        private void CreateFile(FileProps file)
        {
            string newFile = Path.Combine(Config.BackupFolder, file.GetRelativeFilePath);
            File.Copy(file.GetAbsoluteFilePath, newFile);
            _logs.Add(Log(file.GetFileName, newFile, Actions.created));
        }

        private void CheckForDeletedFiles()
        {
            var sourceFileList = GetAllFilesInDirectory(Config.SourceFolder);
            var backupFileList = GetAllFilesInDirectory(Config.BackupFolder);

            foreach (var backupFile in backupFileList)
            {
                var source = sourceFileList.Find(s => s.IsFileTheSame(backupFile));
                if (source == null)
                {
                    DeleteFile(backupFile);
                }
            }
        }

        private void DeleteFile(FileProps file)
        {
            File.Delete(file.GetAbsoluteFilePath);
            _logs.Add(Log(file.GetFileName, file.GetAbsolutePath, Actions.deleted));
        }

        private void CheckForDeletedDirs()
        {
            string[] sourceDirs = Directory.GetDirectories(Config.SourceFolder, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();
            string[] destDirs = Directory.GetDirectories(Config.BackupFolder, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();

            for (int i = destDirs.Length - 1; i >= 0; --i)
            {
                string correspondingSourceDir = destDirs[i].Replace(Config.BackupFolder, Config.SourceFolder);
                if (!Directory.Exists(correspondingSourceDir))
                {
                    Directory.Delete(destDirs[i], true);
                    _logs.Add(Log(destDirs[i], Actions.deleted));
                }
            }
        }

        private List<FileProps> GetAllFilesInDirectory(string root)
        {
            string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(s => s).ToArray();
            return files.Select(file => new FileProps(file, root)).ToList();
        }

        enum Actions
        {
            created,
            deleted,
            copied,
            edited
        }

        

        private string Log(string filename, string fullDestinationName, Actions action)
        {
            string dt = DateTime.Now.ToString("dd-MM-yyyy HH:mm");
            string grammar;
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
                    grammar = "to";
                    break;
                default:
                    grammar = "";
                    break;
            }
            string logTemplate = $"{dt} - {filename} was {action} {grammar} {Path.GetDirectoryName(fullDestinationName)}";
            Console.WriteLine(logTemplate);
            return logTemplate;
        }

        private string Log(string dir, Actions action)
        {
            string dt = DateTime.Now.ToString("dd-MM-yyyy HH:mm");
            string logTemplate = $"{dt} - Directory {dir} was {action}";
            Console.WriteLine(logTemplate);
            return logTemplate;
        }
    }
}