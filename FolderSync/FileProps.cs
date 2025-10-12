using System.Security.Cryptography;

namespace FolderSync
{
    public class FileProps
    {
        private readonly string _fileName;
        private readonly string _root;
        private readonly string _absoluteFilePath;
        private readonly string _absolutePath;
        private readonly string _relativeFilePath;
        private readonly string _relativePath;
        private readonly string _md5Code;

        public FileProps(string file, string sourceDir)
        {
            _fileName = Path.GetFileName(file);
            _root = sourceDir;
            _absoluteFilePath = file;
            _relativeFilePath = file.Replace(sourceDir, "");
            _absolutePath = Path.GetDirectoryName(file);
            _relativePath = _relativeFilePath.Replace(_fileName, "");
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(_absoluteFilePath))
                {
                    var hash = md5.ComputeHash(stream);
                    _md5Code = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public string FileName => this._fileName;
        public string RootFolder => this._root;
        public string AbsoluteFilePath => this._absoluteFilePath;
        public string AbsolutePath => this._absolutePath;
        public string RelativeFilePath => this._relativeFilePath;
        public string RelativePath => this._relativePath;
        public string md5Code => this._md5Code;

        public bool IsFileTheSame(string comparedToPath, string comparedToMD5)
        {
            return this.RelativeFilePath == comparedToPath && IsSameMD5(this.md5Code, comparedToMD5);
        }

        public bool IsFileCopied(string comparedToName, string comparedToMD5)
        {
            return this.FileName == comparedToName && IsSameMD5(this.md5Code, comparedToMD5);
        }
        
        private static bool IsSameMD5(string origin, string backup)
        {
            return String.Equals(origin, backup, StringComparison.OrdinalIgnoreCase);
        }
    }
}