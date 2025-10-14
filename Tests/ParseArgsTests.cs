using System.Reflection;
using Microsoft.Extensions.Configuration;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Tests
{
    public class ParseArgsTests
    {
        [Fact]
        public void TestValidArguments_Succeeds()
        {
            string period = "15";
            string sourceFolder = Path.Combine(Path.GetTempPath(), "SourceFolder");
            string destFolder = Path.Combine(Path.GetTempPath(), "BackupFolder");
            string logFolder = Path.Combine(Path.GetTempPath(), "Logs");
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

            Assert.Equal(Int32.Parse(period), config.SyncPeriod);
            Assert.Equal(sourceFolder, config.SourceFolder);
            Assert.Equal(destFolder, config.BackupFolder);
            Assert.Equal(Path.Combine(logFolder, $"SyncLog_{DateTime.Now.ToString("dd-MM-yyyy")}.log"), config.LogFilePath);

            Directory.Delete(sourceFolder, true);
            Directory.Delete(destFolder, true);
            Directory.Delete(logFolder, true);
        }

        [Fact]
        public void TestInvalidPeriod_DefaultsTo60()
        {
            string period = "-5";
            string sourceFolder = Path.Combine(Path.GetTempPath(), "SourceFolder");
            string destFolder = Path.Combine(Path.GetTempPath(), "BackupFolder");
            string logFolder = Path.Combine(Path.GetTempPath(), "Logs");
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
            Assert.Equal(Path.Combine(logFolder, $"SyncLog_{DateTime.Now.ToString("dd-MM-yyyy")}.log"), config.LogFilePath);

            Directory.Delete(sourceFolder, true);
            Directory.Delete(destFolder, true);
            Directory.Delete(logFolder, true);
        }

        [Fact]
        public void TestMissingSourceFolder_Fails()
        {
            string period = "15";
            string destFolder = Path.Combine(Path.GetTempPath(), "BackupFolder");
            string logFolder = Path.Combine(Path.GetTempPath(), "Logs");
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

            Directory.Delete(destFolder, true);
            Directory.Delete(logFolder, true);
        }

        [Fact]
        public void TestMissingBackupFolder_Fails()
        {
            string period = "15";
            string sourceFolder = Path.Combine(Path.GetTempPath(), "SourceFolder");
            string logFolder = Path.Combine(Path.GetTempPath(), "Logs");
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

            Directory.Delete(sourceFolder, true);
            Directory.Delete(logFolder, true);
        }

        [Fact]
        public void TestNestedPaths_Fails()
        {
            string period = "15";
            string sourceFolder = Path.Combine(Path.GetTempPath(), "SourceFolder");
            string destFolder = Path.Combine(sourceFolder, "BackupFolder");
            string logFolder = Path.Combine(Path.GetTempPath(), "Logs");
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

            Directory.Delete(sourceFolder, true);
            Directory.Delete(logFolder, true);
        }

        [Fact]
        public void TestInvalidLogPath_DefaultsToCurrentDirectory()
        {
            string period = "15";
            string sourceFolder = Path.Combine(Path.GetTempPath(), "SourceFolder");
            string destFolder = Path.Combine(Path.GetTempPath(), "BackupFolder");
            string logFolder = Path.Combine(Path.GetTempPath(), "NonExistentLogs");
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
            Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), $"SyncLog_{DateTime.Now.ToString("dd-MM-yyyy")}.log"), config.LogFilePath);

            Directory.Delete(sourceFolder, true);
            Directory.Delete(destFolder, true);
        }

        [Fact]
        public void TestMissingLogPath_DefaultsToCurrentDirectory()
        {
            string period = "15";
            string sourceFolder = Path.Combine(Path.GetTempPath(), "SourceFolder");
            string destFolder = Path.Combine(Path.GetTempPath(), "BackupFolder");
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

            Directory.Delete(sourceFolder, true);
            Directory.Delete(destFolder, true);
        }

        [Fact]
        public void TestNoArguments_Fails()
        {
            string[] args = Array.Empty<string>();
            IConfiguration cmdArgs = new ConfigurationBuilder().AddCommandLine(args).Build();
            var parseArgs = typeof(FolderSync.FolderSync).GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.Throws<TargetInvocationException>(() => parseArgs.Invoke(null, new object[] { cmdArgs }));
        }
    }
}