using System.Reflection;
using Microsoft.Extensions.Configuration;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Tests
{
    public class ParseArgsTests : IDisposable
    {
        string _testDir;
        public ParseArgsTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "TestRoot");
            Directory.CreateDirectory(_testDir);
        }

        [Fact]
        public void TestValidArguments_Succeeds()
        {
            string period = "15";
            string sourceFolder = Path.Combine(_testDir, "SourceFolder");
            string destFolder = Path.Combine(_testDir, "BackupFolder");
            string logFolder = Path.Combine(_testDir, "Logs");
            if (!Directory.Exists(sourceFolder))
                Directory.CreateDirectory(sourceFolder);
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            Assert.True(Directory.Exists(sourceFolder));
            Assert.True(Directory.Exists(destFolder));
            Assert.True(Directory.Exists(logFolder));

            string[] args = new string[] { "--syncPeriod", period, "--sourceFolder", sourceFolder, "--destFolder", destFolder, "--log", logFolder };
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Config config = (Config)parseArgs.Invoke(null, new object[] { cmdArgs });

            Assert.Equal(Int32.Parse(period), config.SyncPeriod);
            Assert.Equal(sourceFolder, config.SourceFolder);
            Assert.Equal(destFolder, config.BackupFolder);
            string[] logPathAbs = Directory.GetFiles(logFolder, "*.log");
            Assert.Single(logPathAbs);
            string logName = Path.GetFileName(logPathAbs[0]);
            Assert.True(logName.StartsWith("SyncLog_"));
            Assert.Equal(logFolder, config.LogFilePath);
        }

        [Fact]
        public void TestInvalidPeriod_DefaultsTo60()
        {
            string period = "-5";
            string sourceFolder = Path.Combine(_testDir, "SourceFolder");
            string destFolder = Path.Combine(_testDir, "BackupFolder");
            string logFolder = Path.Combine(_testDir, "Logs");
            if (!Directory.Exists(sourceFolder))
                Directory.CreateDirectory(sourceFolder);
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            Assert.True(Directory.Exists(sourceFolder));
            Assert.True(Directory.Exists(destFolder));

            string[] args = new string[] { "--syncPeriod", period, "--sourceFolder", sourceFolder, "--destFolder", destFolder, "--log", logFolder };
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Config config = (Config)parseArgs.Invoke(null, new object[] { cmdArgs });

            Assert.Equal(60, config.SyncPeriod);
            Assert.Equal(sourceFolder, config.SourceFolder);
            Assert.Equal(destFolder, config.BackupFolder);
            string[] logPathAbs = Directory.GetFiles(logFolder, "*.log");
            Assert.Single(logPathAbs);
            string logName = Path.GetFileName(logPathAbs[0]);
            Assert.True(logName.StartsWith("SyncLog_"));
            Assert.Equal(logFolder, config.LogFilePath);
        }

        [Fact]
        public void TestMissingSourceFolder_Fails()
        {
            string period = "15";
            string destFolder = Path.Combine(_testDir, "BackupFolder");
            string logFolder = Path.Combine(_testDir, "Logs");
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            Assert.True(Directory.Exists(destFolder));

            string[] args = new string[] { "--syncPeriod", period, "--destFolder", destFolder, "--log", logFolder };
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var ex = Assert.Throws<TargetInvocationException>(() => parseArgs.Invoke(null, new object[] { cmdArgs }));
            Assert.IsType<ArgumentException>(ex.InnerException);
        }

        [Fact]
        public void TestMissingBackupFolder_Fails()
        {
            string period = "15";
            string sourceFolder = Path.Combine(_testDir, "SourceFolder");
            string logFolder = Path.Combine(_testDir, "Logs");
            if (!Directory.Exists(sourceFolder))
                Directory.CreateDirectory(sourceFolder);
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            Assert.True(Directory.Exists(sourceFolder));

            string[] args = new string[] { "--syncPeriod", period, "--sourceFolder", sourceFolder, "--log", logFolder };
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var ex = Assert.Throws<TargetInvocationException>(() => parseArgs.Invoke(null, new object[] { cmdArgs }));
            Assert.IsType<ArgumentException>(ex.InnerException);
        }

        [Fact]
        public void TestNestedPaths_Fails()
        {
            string period = "15";
            string sourceFolder = Path.Combine(_testDir, "SourceFolder");
            string destFolder = Path.Combine(sourceFolder, "BackupFolder");
            string logFolder = Path.Combine(_testDir, "Logs");
            if (!Directory.Exists(sourceFolder))
                Directory.CreateDirectory(sourceFolder);
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            Assert.True(Directory.Exists(sourceFolder));

            string[] args = new string[] { "--syncPeriod", period, "--sourceFolder", sourceFolder, "--destFolder", destFolder, "--log", logFolder };
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var ex = Assert.Throws<TargetInvocationException>(() => parseArgs.Invoke(null, new object[] { cmdArgs }));
            Assert.IsType<ArgumentException>(ex.InnerException);
        }

        [Fact]
        public void SourcePathCannotBeInProgramDir_Fails()
        {
            string period = "15";
            string sourceFolder = Directory.GetCurrentDirectory();
            string destFolder = Path.Combine(_testDir, "BackupFolder");
            string logFolder = Path.Combine(_testDir, "Logs");
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            Assert.True(Directory.Exists(destFolder));

            string[] args = new string[] { "--syncPeriod", period, "--sourceFolder", sourceFolder, "--destFolder", destFolder, "--log", logFolder };
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var ex = Assert.Throws<TargetInvocationException>(() => parseArgs.Invoke(null, new object[] { cmdArgs }));
            Assert.IsType<ArgumentException>(ex.InnerException);
        }

        [Fact]
        public void BackupPathCannotBeInProgramDir_Fails()
        {
            string period = "15";
            string sourceFolder = Path.Combine(_testDir, "SourceFolder");
            string destFolder = Path.Combine(Directory.GetCurrentDirectory(), "BackupFolder");
            string logFolder = Path.Combine(_testDir, "Logs");
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            Assert.True(Directory.Exists(destFolder));

            string[] args = new string[] { "--syncPeriod", period, "--sourceFolder", sourceFolder, "--destFolder", destFolder, "--log", logFolder };
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var ex = Assert.Throws<TargetInvocationException>(() => parseArgs.Invoke(null, new object[] { cmdArgs }));
            Assert.IsType<ArgumentException>(ex.InnerException);
        }

        [Fact]
        public void TestInvalidLogPath_DefaultsToCurrentDirectory()
        {
            string period = "15";
            string sourceFolder = Path.Combine(_testDir, "SourceFolder");
            string destFolder = Path.Combine(_testDir, "BackupFolder");
            string logFolder = Path.Combine(_testDir, "NonExistentLogs");
            if (!Directory.Exists(sourceFolder))
                Directory.CreateDirectory(sourceFolder);
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            Assert.True(Directory.Exists(sourceFolder));
            Assert.True(Directory.Exists(destFolder));

            string[] args = new string[] { "--syncPeriod", period, "--sourceFolder", sourceFolder, "--destFolder", destFolder, "--log", logFolder };
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Config config = (Config)parseArgs.Invoke(null, new object[] { cmdArgs });

            Assert.Equal(Int32.Parse(period), config.SyncPeriod);
            Assert.Equal(sourceFolder, config.SourceFolder);
            Assert.Equal(destFolder, config.BackupFolder);

            string[] finalLog = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.log");
            Assert.Single(finalLog);
            string logName = Path.GetFileName(finalLog[0]);
            Assert.True(logName.StartsWith("SyncLog_"));
            Assert.Equal(Directory.GetCurrentDirectory(), config.LogFilePath);
        }

        [Fact]
        public void TestMissingLogPath_DefaultsToCurrentDirectory()
        {
            string period = "15";
            string sourceFolder = Path.Combine(_testDir, "SourceFolder");
            string destFolder = Path.Combine(_testDir, "BackupFolder");
            if (!Directory.Exists(sourceFolder))
                Directory.CreateDirectory(sourceFolder);
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            Assert.True(Directory.Exists(sourceFolder));
            Assert.True(Directory.Exists(destFolder));

            string[] args = new string[] { "--syncPeriod", period, "--sourceFolder", sourceFolder, "--destFolder", destFolder };
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Config config = (Config)parseArgs.Invoke(null, new object[] { cmdArgs });

            Assert.Equal(Int32.Parse(period), config.SyncPeriod);
            Assert.Equal(sourceFolder, config.SourceFolder);
            Assert.Equal(destFolder, config.BackupFolder);
            Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), $"SyncLog_{DateTime.Now.ToString("dd-MM-yyyy")}.log"), config.LogFilePath);
        }

        [Fact]
        public void TestNestedLogFileInSource()
        {
            string period = "15";
            string sourceFolder = Path.Combine(_testDir, "SourceFolder");
            string destFolder = Path.Combine(_testDir, "BackupFolder");
            string logFolder = Path.Combine(_testDir, sourceFolder);
            if (!Directory.Exists(sourceFolder))
                Directory.CreateDirectory(sourceFolder);
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            Assert.True(Directory.Exists(sourceFolder));

            string[] args = new string[] { "--syncPeriod", period, "--sourceFolder", sourceFolder, "--destFolder", destFolder, "--log", logFolder };
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Config config = (Config)parseArgs.Invoke(null, new object[] { cmdArgs });

            string fixedLogFolder = Directory.GetCurrentDirectory();
            Assert.Equal(fixedLogFolder, config.LogFilePath);
            string[] finalLog = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.log");
            Assert.Single(finalLog);
            string logName = Path.GetFileName(finalLog[0]);
            Assert.True(logName.StartsWith("SyncLog_"));
        }

        [Fact]
        public void TestNoArguments_Fails()
        {
            string[] args = Array.Empty<string>();
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.Throws<TargetInvocationException>(() => parseArgs.Invoke(null, new object[] { cmdArgs }));
        }

        public void Dispose()
        {
            Directory.Delete(_testDir, true);
            string[] logFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.log");
            foreach (var log in logFiles)
                File.Delete(log);
        }
    }
}