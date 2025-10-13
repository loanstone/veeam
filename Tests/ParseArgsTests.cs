using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Xunit;
using FolderSync;

namespace Tests
{
    public class ParseArgsTests
    {
        [Theory]
        [InlineData("--badSyncArgName")]
        [InlineData("- syncPeriod")]
        [InlineData("/SyncPeriod=")]
        [InlineData("SyncPeriod", "1")]
        [InlineData("--SyncPeriod", "1")]
        public void SetSyncPeriodArgument_Fails(params string[] args)
        {
            IConfiguration arguments = new ConfigurationBuilder().AddCommandLine(args).Build();

            var type = typeof(FolderSync.FolderSync);
            var method = type.GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var syncPeriodField = type.GetField("syncPeriod", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { arguments });
            Assert.Equal(0, syncPeriodField.GetValue(null));
        }

        [Theory]
        [InlineData("--syncPeriod", "1")]
        [InlineData("--syncPeriod=", "10")]
        [InlineData("--syncPeriod=15", "1")]
        public void SetSyncArgumentValue_Succeeds(params string[] args)
        {
            IConfiguration arguments = new ConfigurationBuilder().AddCommandLine(args).Build();

            var type = typeof(FolderSync.FolderSync);
            var method = type.GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var syncPeriodField = type.GetField("syncPeriod", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { arguments });
            Assert.InRange((int)syncPeriodField.GetValue(null), 1, int.MaxValue);
        }

        [Theory]
        [InlineData("--syncPeriod", "")]
        [InlineData("--syncPeriod", " ")]
        [InlineData("--syncPeriod", "abc")]
        [InlineData("--syncPeriod", "-1")]
        [InlineData("--syncPeriod", "0")]
        [InlineData("--syncPeriod", "1.5")]
        [InlineData("--syncPeriod", "1.0")]
        [InlineData("--syncPeriod", "1,0")]
        [InlineData("--syncPeriod=0", "2")]
        [InlineData("--syncPeriod=", "0")]
        
        public void SetSyncArgumentValue_DefaultsToSixty(params string[] args)
        {
            IConfiguration arguments = new ConfigurationBuilder().AddCommandLine(args).Build();

            var type = typeof(FolderSync.FolderSync);
            var method = type.GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var syncPeriodField = type.GetField("syncPeriod", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { arguments });
            Assert.Equal(60, syncPeriodField.GetValue(null));
        }

        [Theory]
        [InlineData("--badSourceRootArgName")]
        [InlineData("- sourceRoot")]
        [InlineData("/sourceRoot=")]
        [InlineData("sourceRoot", "")]
        [InlineData("sourceRoot", "/tmp/NonExistentFolder")]
        [InlineData("sourceRoot", "C:\\ThisFolderShouldNotExist")]
        [InlineData("--sourceRoot", "Invalid<>Path|Name")]
        [InlineData("--sourceRoot", "/")]
        [InlineData("--sourceRoot", "../")]
        public void SetSourceRootArgument_Fails(params string[] args)
        {
            IConfiguration arguments = new ConfigurationBuilder().AddCommandLine(args).Build();

            var type = typeof(FolderSync.FolderSync);
            var method = type.GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var sourceRootField = type.GetField("sourceRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { arguments });
            Assert.Null(sourceRootField.GetValue(null));
        }

        [Fact]
        public void SetSourceRootArgument_Succeeds()
        {
            string tempPath = Path.GetTempPath();
            Directory.CreateDirectory(Path.Combine(tempPath, "TestSourceRoot"));
            string testSourcePath = Path.Combine(tempPath, "TestSourceRoot");
            Assert.True(Directory.Exists(testSourcePath));
            IConfiguration arguments = new ConfigurationBuilder().AddCommandLine(new string[] { "--sourceFolder", testSourcePath }).Build();

            var type = typeof(FolderSync.FolderSync);
            var method = type.GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var sourceRootField = type.GetField("sourceRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { arguments });
            Assert.Equal(testSourcePath, sourceRootField.GetValue(null));

            Assert.True(Directory.Exists(testSourcePath));
            Directory.Delete(testSourcePath, true);
            Assert.False(Directory.Exists(testSourcePath));
        }

        [Theory]
        [InlineData("--badBackupRootArgName")]
        [InlineData("- destFolder")]
        [InlineData("/destFolder=")]
        [InlineData("destFolder", "")]
        [InlineData("destFolder", "/tmp/NonExistentFolder")]
        [InlineData("destFolder", "C:\\ThisFolderShouldNotExist")]
        [InlineData("--destFolder", "Invalid<>Path|Name")]
        [InlineData("--destFolder", "/")]
        [InlineData("--destFolder", "../")]
        public void SetBackupRootArgument_Fails(params string[] args)
        {
            IConfiguration arguments = new ConfigurationBuilder().AddCommandLine(args).Build();

            var type = typeof(FolderSync.FolderSync);
            var method = type.GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var backupRootField = type.GetField("backupRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var exceptions = Record.Exception(() => method.Invoke(null, new object[] { arguments }));
            Assert.NotNull(exceptions);
            Assert.Null(backupRootField.GetValue(null));
        }

        [Fact]
        public void SetBackupRootArgument_Succeeds()
        {
            string tempPath = Path.GetTempPath();
            Directory.CreateDirectory(Path.Combine(tempPath, "TestSourceRoot"));
            string testBackupPath = Path.Combine(tempPath, "TestSourceRoot");
            Assert.True(Directory.Exists(testBackupPath));
            IConfiguration arguments = new ConfigurationBuilder().AddCommandLine(new string[] { "--sourceFolder", testBackupPath }).Build();

            var type = typeof(FolderSync.FolderSync);
            var method = type.GetMethod("ParseArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var sourceRootField = type.GetField("sourceRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { arguments });
            Assert.Equal(testBackupPath, sourceRootField.GetValue(null));

            Assert.True(Directory.Exists(testBackupPath));
            Directory.Delete(testBackupPath, true);
            Assert.False(Directory.Exists(testBackupPath));
        }
    }
}