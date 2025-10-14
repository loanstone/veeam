using System.Security.Cryptography;

namespace Tests
{
    public class CompleteSyncTest : IDisposable
    {
        private readonly string _testDir;
        Config config;
        public CompleteSyncTest()
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
        public void TestInitalSync()
        {
            #region setup
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceFile1 = Path.Combine(config.SourceFolder, "File1.txt");
            string sourceFile2 = Path.Combine(config.SourceFolder, "File2.txt");
            string sourceSubDir1 = Path.Combine(config.SourceFolder, "SubDir1");
            string sourceFile3 = Path.Combine(sourceSubDir1, "File1.txt");
            string sourceSubDir2 = Path.Combine(sourceSubDir1, "SubDir2");

            File.Create(sourceFile1).Close();
            File.Create(sourceFile2).Close();
            Directory.CreateDirectory(sourceSubDir1);
            File.Create(sourceFile3).Close();
            Directory.CreateDirectory(sourceSubDir2);

            Assert.True(File.Exists(sourceFile1));
            Assert.True(File.Exists(sourceFile2));
            Assert.True(Directory.Exists(sourceSubDir1));
            Assert.True(File.Exists(sourceFile3));
            Assert.True(Directory.Exists(sourceSubDir2));

            string backupFile1 = Path.Combine(config.BackupFolder, "RandomFile1.txt");
            string backupFile2 = Path.Combine(config.BackupFolder, "RandomFile2.txt");
            string backupSubDir1 = Path.Combine(config.BackupFolder, "SubDir1");
            string backupFile3 = Path.Combine(backupSubDir1, "File1.txt");
            string backupSubDir2 = Path.Combine(backupSubDir1, "RandomSubDir");
            string backupFile4 = Path.Combine(backupSubDir2, "RandomFile3.txt");

            File.Create(backupFile1).Close();
            File.Create(backupFile2).Close();
            Directory.CreateDirectory(backupSubDir1);
            File.Create(backupFile3).Close();
            Directory.CreateDirectory(backupSubDir2);
            File.Create(backupFile4).Close();

            Assert.True(File.Exists(backupFile1));
            Assert.True(File.Exists(backupFile2));
            Assert.True(Directory.Exists(backupSubDir1));
            Assert.True(File.Exists(backupFile3));
            Assert.True(Directory.Exists(backupSubDir2));
            Assert.True(File.Exists(backupFile4));
            #endregion

            syncLogic.RunInitialSync();

            Assert.True(File.Exists(Path.Combine(config.BackupFolder, "File1.txt")));
            Assert.True(File.Exists(Path.Combine(config.BackupFolder, "File2.txt")));
            Assert.True(Directory.Exists(Path.Combine(config.BackupFolder, "SubDir1")));
            Assert.True(File.Exists(Path.Combine(config.BackupFolder, "SubDir1", "File1.txt")));
            Assert.True(Directory.Exists(Path.Combine(config.BackupFolder, "SubDir1", "SubDir2")));
            Assert.True(File.Exists(backupFile3));
            Assert.False(File.Exists(backupFile1));
            Assert.False(File.Exists(backupFile2));
            Assert.False(Directory.Exists(backupSubDir2));
            Assert.False(File.Exists(backupFile4));
        }

        [Fact]
        public void RunCompleteSync()
        {
            #region setup
            SyncLogic syncLogic = new SyncLogic(config);

            string sourceFile1 = Path.Combine(config.SourceFolder, "File1.txt"); // existing synced file - should pass in IsFileTheSame()
            string sourceFile2 = Path.Combine(config.SourceFolder, "File2.txt"); // this is a modification of an already synced file - should pass in IsFileModified()
            string sourceSubDir1 = Path.Combine(config.SourceFolder, "SubDir1"); // this directory already exists in backup - should pass in IsDirectoryTheSame()
            string sourceFile3 = Path.Combine(sourceSubDir1, "File1.txt"); // copy of sourceFile1 - should pass in IsFileCopied()
            string sourceSubDir2 = Path.Combine(sourceSubDir1, "SubDir2"); // new directory that doesn't exist in backup - should be created
            string sourceFile4 = Path.Combine(sourceSubDir2, "File3.txt"); // new file - should be synced as new

            File.Create(sourceFile1).Close();
            File.Create(sourceFile2).Close();
            File.WriteAllLines(sourceFile2, ["Edited"]);
            Directory.CreateDirectory(sourceSubDir1);
            File.Create(sourceFile3).Close();
            Directory.CreateDirectory(sourceSubDir2);
            File.Create(sourceFile4).Close();

            Assert.True(File.Exists(sourceFile1));
            Assert.True(File.Exists(sourceFile2));
            Assert.True(Directory.Exists(sourceSubDir1));
            Assert.True(File.Exists(sourceFile3));
            Assert.True(Directory.Exists(sourceSubDir2));
            Assert.True(File.Exists(sourceFile4));

            string backupFile1 = Path.Combine(config.BackupFolder, "File1.txt"); // same as sourceFile1 - should pass in IsFileTheSame()
            string backupFile2 = Path.Combine(config.SourceFolder, "File2.txt"); // this file has been modified since the last sync - should pass in IsFileModified()
            string backupSubDir1 = Path.Combine(config.BackupFolder, "SubDir1"); // already exists - should pass in IsDirectoryTheSame()
            string backupSubDir2 = Path.Combine(config.BackupFolder, "ThisFolderDoesntExistInSource"); // should be deleted
            string backupFile4 = Path.Combine(backupSubDir1, "RandomFile3.txt"); // should be deleted

            File.Create(backupFile1).Close();
            File.Create(backupFile2).Close();
            File.WriteAllLines(backupFile2, ["Original"]);
            Directory.CreateDirectory(backupSubDir1);
            File.Create(backupFile4).Close();
            Directory.CreateDirectory(backupSubDir2);

            Assert.True(File.Exists(backupFile1));
            Assert.True(File.Exists(backupFile2));
            Assert.True(Directory.Exists(backupSubDir1));
            Assert.True(File.Exists(backupFile4));
            Assert.True(Directory.Exists(backupSubDir2));
            #endregion

            #region execute methods
            var type = typeof(SyncLogic);

            var missingDirs = type.GetMethod("CheckForMissingDirs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            missingDirs.Invoke(syncLogic, null);

            var modifiedFiles = type.GetMethod("CheckForModifiedFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            modifiedFiles.Invoke(syncLogic, null);

            var copiesInSource = type.GetMethod("CheckForCopiesInSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            copiesInSource.Invoke(syncLogic, null);

            var missingFiles = type.GetMethod("CheckForMissingFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            missingFiles.Invoke(syncLogic, null);

            var deletedFiles = type.GetMethod("CheckForDeletedFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            deletedFiles.Invoke(syncLogic, null);

            var deletedDirs = type.GetMethod("CheckForDeletedDirs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            deletedDirs.Invoke(syncLogic, null);
            #endregion

            Assert.True(File.Exists(Path.Combine(config.BackupFolder, "File1.txt")));
            Assert.True(AreFilesTheSame(sourceFile1, Path.Combine(config.BackupFolder, "File1.txt")));
            Assert.True(File.Exists(Path.Combine(config.BackupFolder, "File2.txt")));
            Assert.True(AreFilesTheSame(sourceFile2, Path.Combine(config.BackupFolder, "File2.txt")));
            Assert.True(Directory.Exists(Path.Combine(config.BackupFolder, "SubDir1")));
            Assert.True(File.Exists(Path.Combine(config.BackupFolder, "SubDir1", "File1.txt")));
            Assert.True(AreFilesTheSame(sourceFile3, Path.Combine(config.BackupFolder, "SubDir1", "File1.txt")));
            Assert.True(Directory.Exists(Path.Combine(config.BackupFolder, "SubDir1", "SubDir2")));
            Assert.True(File.Exists(Path.Combine(config.BackupFolder, "SubDir1", "SubDir2", "File3.txt")));
            Assert.True(AreFilesTheSame(sourceFile4, Path.Combine(config.BackupFolder, "SubDir1", "SubDir2", "File3.txt")));
            Assert.False(Directory.Exists(backupSubDir2));
            Assert.False(File.Exists(backupFile4));
        }

        private bool AreFilesTheSame(string originalFile, string backupFile)
        {
            return GenerateMD5(originalFile) == GenerateMD5(backupFile);
        }
        
        private string GenerateMD5 (string file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public void Dispose()
        {
            Directory.Delete(_testDir, true);
        }
    }
}