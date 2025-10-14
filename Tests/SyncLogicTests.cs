namespace Tests
{

    public class SyncLogicTests : IDisposable
    {
        private readonly string _testDir;
        Config config;
        public SyncLogicTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "TestRoot");
            Directory.CreateDirectory(_testDir);
            Directory.CreateDirectory(Path.Combine(_testDir, "TestLogFolder"));
            string logFile = Path.Combine(_testDir, "TestLogFolder", "log.log");
            File.Create(logFile).Close();
            config = new Config
            {
                SourceFolder = Directory.CreateDirectory(Path.Combine(_testDir, "TestSourceRoot")).FullName,
                BackupFolder = Directory.CreateDirectory(Path.Combine(_testDir, "TestBackupRoot")).FullName,
                SyncPeriod = 1,
                LogFilePath = logFile
            };
        }

        [Fact]
        public void CheckForMissingDirsTest_SameStructure()
        {
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceDirOne = Path.Combine(config.SourceFolder, "NewSourceFolder");
            string sourceDirTwo = Path.Combine(sourceDirOne, "NewSourceSubFolder");
            Directory.CreateDirectory(sourceDirOne);
            Directory.CreateDirectory(sourceDirTwo);

            string backupDirOne = Path.Combine(config.BackupFolder, "NewSourceFolder");
            string backupDirTwo = Path.Combine(backupDirOne, "NewSourceSubFolder");
            Directory.CreateDirectory(backupDirOne);
            Directory.CreateDirectory(backupDirTwo);

            var type = typeof(SyncLogic);
            var method = type.GetMethod("CheckForMissingDirs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(syncLogic, null);

            Assert.True(Directory.Exists(Path.Combine(config.BackupFolder, "NewSourceFolder")));
            Assert.True(Directory.Exists(Path.Combine(config.BackupFolder, "NewSourceFolder", "NewSourceSubFolder")));
        }

        [Fact]
        public void CheckForMissingDirsTest_DifferentStructure()
        {
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceDirOne = Path.Combine(config.SourceFolder, "NewSourceFolder");
            string sourceDirTwo = Path.Combine(sourceDirOne, "NewSourceSubFolder");
            Directory.CreateDirectory(sourceDirOne);
            Directory.CreateDirectory(sourceDirTwo);

            string backupDirOne = Path.Combine(config.BackupFolder, "NewBkpFolder_one");
            string backupDirTwo = Path.Combine(config.BackupFolder, "NewBkpSubFolder_two");
            Directory.CreateDirectory(backupDirOne);
            Directory.CreateDirectory(backupDirTwo);

            var type = typeof(SyncLogic);
            var method = type.GetMethod("CheckForMissingDirs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(syncLogic, null);

            Assert.True(Directory.Exists(Path.Combine(config.BackupFolder, "NewSourceFolder")));
            Assert.True(Directory.Exists(Path.Combine(config.BackupFolder, "NewSourceFolder", "NewSourceSubFolder")));
        }

        [Fact]
        public void CheckForModifiedFiles_SourceModified()
        {
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceFile = Path.Combine(config.SourceFolder, "SourceFile.txt");
            string backupFile = Path.Combine(config.BackupFolder, "SourceFile.txt");

            File.Create(sourceFile).Close();
            File.Create(backupFile).Close();

            File.WriteAllLines(sourceFile, ["New Line"]);

            var type = typeof(SyncLogic);
            var method = type.GetMethod("CheckForModifiedFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(syncLogic, null);

            Assert.Equal(File.ReadAllLines(sourceFile), File.ReadAllLines(backupFile));
        }

        [Fact]
        public void CheckForModifiedFiles_NoneModified()
        {
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceFile = Path.Combine(config.SourceFolder, "SourceFile.txt");
            string backupFile = Path.Combine(config.BackupFolder, "SourceFile.txt");

            File.Create(sourceFile).Close();
            File.Create(backupFile).Close();

            var type = typeof(SyncLogic);
            var method = type.GetMethod("CheckForModifiedFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(syncLogic, null);

            Assert.Equal(File.ReadAllLines(sourceFile), File.ReadAllLines(backupFile));
        }

        [Fact]
        public void CheckForModifiedFiles_BackupModified()
        {
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceFile = Path.Combine(config.SourceFolder, "SourceFile.txt");
            string backupFile = Path.Combine(config.BackupFolder, "SourceFile.txt");

            File.Create(sourceFile).Close();
            File.Create(backupFile).Close();

            File.WriteAllLines(backupFile, ["New Line"]);

            var type = typeof(SyncLogic);
            var method = type.GetMethod("CheckForModifiedFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(syncLogic, null);

            Assert.Equal(File.ReadAllLines(sourceFile), File.ReadAllLines(backupFile));
        }

        [Fact]
        public void CheckForCopiesInSource()
        {
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceFile = Path.Combine(config.SourceFolder, "SourceFile.txt");
            string sourceSubDir = Path.Combine(config.SourceFolder, "SubDir");
            string sourceFile_Copy = Path.Combine(sourceSubDir, "SourceFile.txt");
            string backupFile = Path.Combine(config.BackupFolder, "SourceFile.txt");
            string backupSubDir = Path.Combine(config.BackupFolder, "SubDir");
            string backupFile_Copy = Path.Combine(backupSubDir, "SourceFile.txt");

            File.Create(sourceFile).Close();
            Directory.CreateDirectory(sourceSubDir);
            File.Create(sourceFile_Copy).Close();
            File.Create(backupFile).Close();
            Directory.CreateDirectory(backupSubDir);

            var type = typeof(SyncLogic);
            var method = type.GetMethod("CheckForCopiesInSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(syncLogic, null);

            Assert.True(File.Exists(backupFile_Copy));
        }

        [Fact]
        public void CheckForMissingFiles()
        {
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceFile_One = Path.Combine(config.SourceFolder, "SourceFile1.txt");
            string sourceFile_Two = Path.Combine(config.SourceFolder, "SourceFile2.txt");
            string backupFile_One = Path.Combine(config.BackupFolder, "SourceFile1.txt");
            string backupFile_Two = Path.Combine(config.BackupFolder, "SourceFile2.txt");

            File.Create(sourceFile_One).Close();
            File.Create(sourceFile_Two).Close();

            var type = typeof(SyncLogic);
            var method = type.GetMethod("CheckForMissingFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(syncLogic, null);

            Assert.True(File.Exists(backupFile_One));
            Assert.True(File.Exists(backupFile_Two));
        }

        [Fact]
        public void CheckForDeletedFiles()
        {
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceFile = Path.Combine(config.SourceFolder, "SourceFile.txt");
            string backupFile_One = Path.Combine(config.BackupFolder, "SourceFile.txt");
            string backupFile_Two = Path.Combine(config.BackupFolder, "AnotherFile.txt");

            File.Create(sourceFile).Close();
            File.Create(backupFile_One).Close();
            File.Create(backupFile_Two).Close();

            var type = typeof(SyncLogic);
            var method = type.GetMethod("CheckForDeletedFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(syncLogic, null);

            Assert.True(File.Exists(backupFile_One));
            Assert.False(File.Exists(backupFile_Two));
        }

        [Fact]
        public void CheckForDeletedDirs()
        {
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceDir = Path.Combine(config.SourceFolder, "SourceDir");
            string backupDir_One = Path.Combine(config.BackupFolder, "SourceDir");
            string backupDir_Two = Path.Combine(config.BackupFolder, "AnotherDir");
            string backupFile = Path.Combine(backupDir_Two, "ToBeDeleted.txt");

            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(backupDir_One);
            Directory.CreateDirectory(backupDir_Two);
            File.Create(Path.Combine(backupDir_Two, backupFile)).Close();

            var type = typeof(SyncLogic);
            var method = type.GetMethod("CheckForDeletedDirs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(syncLogic, null);

            Assert.True(Directory.Exists(backupDir_One));
            Assert.False(Directory.Exists(backupDir_Two));
        }

        [Fact]
        public void AreLogsWritten()
        {
            SyncLogic syncLogic = new SyncLogic(config);

            string logLine = "This is written to the log file";

            var type = typeof(SyncLogic);
            var logsField = type.GetField("_logs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var logList = (List<string>)logsField.GetValue(syncLogic);
            logList.Add(logLine);
            var method = type.GetMethod("WriteLogs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(syncLogic, null);

            string logFilePath = Path.Combine(config.LogFilePath);
            var logContents = File.ReadAllLines(logFilePath);
            foreach (var line in logContents)
            {
                Assert.Equal(logLine, line);
            }
        }

        public void Dispose()
        {
            Directory.Delete(_testDir, true);
        }
    }
}