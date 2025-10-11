using System.Security.Cryptography;

namespace FolderSync
{
    public class FileProps
    {
        private readonly string _fileName;
        private readonly string _absoluteFilePath;
        private readonly string _absolutePath;
        private readonly string _relativeFilePath;
        private readonly string _relativePath;
        private readonly string _md5Code;

        public FileProps(string file, string sourceDir)
        {
            _fileName = Path.GetFileName(file);
            _absoluteFilePath = file;
            _relativeFilePath = file.Replace(sourceDir, "");
            _absolutePath = Path.GetDirectoryName(file);
            _relativePath = _relativeFilePath.Replace(_fileName, "");
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(_absoluteFilePath))
                {
                    var hash = md5.ComputeHash(stream);
                    _md5Code = BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }

        public string FileName => _fileName;
        public string AbsoluteFilePath => _absoluteFilePath;
        public string AbsolutePath => _absolutePath;
        public string RelativeFilePath => _relativeFilePath;
        public string RelativePath => _relativePath;
        public string md5Code => _md5Code;
    }
}