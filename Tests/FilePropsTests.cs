using System.Security.Cryptography;

namespace Tests
{
    public class FilePropsTests : IDisposable
    {
        private readonly string sourceRoot;
        private readonly string backupRoot;

        public FilePropsTests()
        {
            sourceRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "TestSourceRoot")).FullName;
            Assert.True(Directory.Exists(sourceRoot));
            backupRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "TestBackupRoot")).FullName;
            Assert.True(Directory.Exists(backupRoot));
        }

        private string CalculateMd5(string absoluteFilePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(absoluteFilePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        [Fact]
        public void FileObjectIsCreated_Succeeds()
        {
            string testFilePath = Path.Combine(sourceRoot, "SampleFile.txt");
            File.Create(testFilePath).Close();
            Assert.True(File.Exists(testFilePath));

            FileProps file = new FileProps(testFilePath, sourceRoot);
            Assert.Equal("SampleFile.txt", file.GetFileName);
            Assert.Equal(sourceRoot, file.GetRootFolder);
            Assert.Equal(testFilePath, file.GetAbsoluteFilePath);
            Assert.Equal(sourceRoot, file.GetAbsolutePath);
            Assert.Equal(Path.Combine(Path.GetRelativePath(sourceRoot, testFilePath)), file.GetRelativeFilePath);
            Assert.Equal(Path.GetDirectoryName(Path.GetRelativePath(sourceRoot, testFilePath)), file.GetRelativePath);
            string expectedMd5 = CalculateMd5(testFilePath);
            Assert.Equal(expectedMd5, file.GetMD5Code);

            File.Delete(testFilePath);
            Assert.False(File.Exists(testFilePath));

            Directory.Delete(sourceRoot, true);
            Assert.False(Directory.Exists(sourceRoot));
        }

        [Fact]
        public void FileObjectIsCreated_Fails()
        {
            string testFilePath = Path.Combine(sourceRoot, "SampleFile.txt");
            Assert.False(File.Exists(testFilePath));

            Exception ex = Assert.Throws<FileNotFoundException>(() => new FileProps(testFilePath, sourceRoot));
            Assert.Equal("Could not find file '" + testFilePath + "'.", ex.Message);

            Directory.Delete(sourceRoot, true);
            Assert.False(Directory.Exists(sourceRoot));
        }

        [Fact]
        public void IsFileTheSame_Succeeds()
        {
            string sourceFilePath = Path.Combine(sourceRoot, "SampleFile.txt");
            File.Create(sourceFilePath).Close();
            string backupFilePath = Path.Combine(backupRoot, "SampleFile.txt");
            File.Create(backupFilePath).Close();
            Assert.True(File.Exists(sourceFilePath));
            Assert.True(File.Exists(backupFilePath));

            FileProps source = new FileProps(sourceFilePath, sourceRoot);
            FileProps backup = new FileProps(backupFilePath, backupRoot);
            Assert.True(source.IsFileTheSame(backup));

            File.Delete(sourceFilePath);
            File.Delete(backupFilePath);
            Assert.False(File.Exists(sourceFilePath));
            Assert.False(File.Exists(backupFilePath));

            Directory.Delete(sourceRoot, true);
            Directory.Delete(backupRoot, true);
            Assert.False(Directory.Exists(sourceRoot));
            Assert.False(Directory.Exists(backupRoot));
        }

        [Fact]
        public void IsFileTheSame_Fails()
        {
            string sourceFilePath = Path.Combine(sourceRoot, "SampleFile.txt");
            File.Create(sourceFilePath).Close();
            File.WriteAllText(sourceFilePath, "The source file is different");
            string backupFilePath = Path.Combine(backupRoot, "SampleFile.txt");
            File.Create(backupFilePath).Close();
            Assert.True(File.Exists(sourceFilePath));
            Assert.True(File.Exists(backupFilePath));

            FileProps source = new FileProps(sourceFilePath, sourceRoot);
            FileProps backup = new FileProps(backupFilePath, backupRoot);
            Assert.False(source.IsFileTheSame(backup));

            File.Delete(sourceFilePath);
            File.Delete(backupFilePath);
            Assert.False(File.Exists(sourceFilePath));
            Assert.False(File.Exists(backupFilePath));

            Directory.Delete(sourceRoot, true);
            Directory.Delete(backupRoot, true);
            Assert.False(Directory.Exists(sourceRoot));
            Assert.False(Directory.Exists(backupRoot));
        }

        [Fact]
        public void IsFileCopied_Succeeds()
        {
            string sourceFilePath = Path.Combine(sourceRoot, "SampleFile.txt");
            File.Create(sourceFilePath).Close();
            Directory.CreateDirectory(Path.Combine(sourceRoot, "Subfolder"));
            string sourceFileCopyPath = Path.Combine(sourceRoot, "Subfolder", "SampleFile.txt");
            File.Copy(sourceFilePath, sourceFileCopyPath);
            Assert.True(File.Exists(sourceFilePath));
            Assert.True(File.Exists(sourceFileCopyPath));
            FileProps source = new FileProps(sourceFilePath, sourceRoot);
            FileProps sourceCopy = new FileProps(sourceFileCopyPath, sourceRoot);
            Assert.True(source.IsFileCopied(sourceCopy));

            File.Delete(sourceFilePath);
            Assert.False(File.Exists(sourceFilePath));
            File.Delete(sourceFileCopyPath);
            Assert.False(File.Exists(sourceFileCopyPath));
            Directory.Delete(Path.Combine(sourceRoot, "Subfolder"));
            Assert.False(Directory.Exists(Path.Combine(sourceRoot, "Subfolder")));

            Directory.Delete(sourceRoot, true);
            Assert.False(Directory.Exists(sourceRoot));
        }

        [Fact]
        public void IsFileCopied_Fails()
        {
            string sourceFilePath = Path.Combine(sourceRoot, "SampleFile.txt");
            File.Create(sourceFilePath).Close();
            string sourceFileCopyPath = Path.Combine(sourceRoot, "SampleFileCopy.txt");
            File.Copy(sourceFilePath, sourceFileCopyPath);
            Assert.True(File.Exists(sourceFilePath));
            Assert.True(File.Exists(sourceFileCopyPath));

            FileProps source = new FileProps(sourceFilePath, sourceRoot);
            FileProps sourceCopy = new FileProps(sourceFileCopyPath, sourceRoot);
            Assert.False(source.IsFileCopied(sourceCopy));

            File.Delete(sourceFilePath);
            Assert.False(File.Exists(sourceFilePath));
            File.Delete(sourceFileCopyPath);
            Assert.False(File.Exists(sourceFileCopyPath));

            Directory.Delete(sourceRoot, true);
            Assert.False(Directory.Exists(sourceRoot));
        }

        [Fact]
        public void IsFileModified_Succeeds()
        {
            string sourceFilePath = Path.Combine(sourceRoot, "SampleFile.txt");
            File.Create(sourceFilePath).Close();
            File.WriteAllText(sourceFilePath, "The source file is different");
            string backupFilePath = Path.Combine(backupRoot, "SampleFile.txt");
            File.Copy(sourceFilePath, backupFilePath);
            Assert.True(File.Exists(sourceFilePath));
            Assert.True(File.Exists(backupFilePath));

            FileProps source = new FileProps(sourceFilePath, sourceRoot);
            FileProps backup = new FileProps(backupFilePath, sourceRoot);
            Assert.False(source.IsFileModified(backup));

            File.Delete(sourceFilePath);
            Assert.False(File.Exists(sourceFilePath));
            File.Delete(backupFilePath);
            Assert.False(File.Exists(backupFilePath));

            Directory.Delete(sourceRoot, true);
            Assert.False(Directory.Exists(sourceRoot));
            Directory.Delete(backupRoot, true);
            Assert.False(Directory.Exists(backupRoot));
        }

        [Fact]
        public void IsFileModified_Fails()
        {
            string sourceFilePath = Path.Combine(sourceRoot, "SampleFile.txt");
            File.Create(sourceFilePath).Close();
            string backupFilePath = Path.Combine(backupRoot, "SampleFile.txt");
            File.Create(backupFilePath).Close();
            Assert.True(File.Exists(sourceFilePath));
            Assert.True(File.Exists(backupFilePath));

            FileProps source = new FileProps(sourceFilePath, sourceRoot);
            FileProps backup = new FileProps(backupFilePath, sourceRoot);
            Assert.False(source.IsFileModified(backup));

            File.Delete(sourceFilePath);
            Assert.False(File.Exists(sourceFilePath));
            File.Delete(backupFilePath);
            Assert.False(File.Exists(backupFilePath));
        }

        public void Dispose()
        {
            if (Directory.Exists(sourceRoot))
            {
                Directory.Delete(sourceRoot, true);
                Assert.False(Directory.Exists(sourceRoot));
            }
            if (Directory.Exists(backupRoot))
            {
                Directory.Delete(backupRoot, true);
                Assert.False(Directory.Exists(backupRoot));
            }
        }
    }
}