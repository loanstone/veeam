using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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

        public FileProps(string absoluteFilePath, string root)
        {
            _fileName = Path.GetFileName(absoluteFilePath);
            _root = root;
            _absoluteFilePath = absoluteFilePath;
            _relativeFilePath = absoluteFilePath.Replace(root, "");
            _absolutePath = Path.GetDirectoryName(absoluteFilePath);
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

        public string GetFileName => this._fileName;
        public string GetRootFolder => this._root;
        public string GetAbsoluteFilePath => this._absoluteFilePath;
        public string GetAbsolutePath => this._absolutePath;
        public string GetRelativeFilePath => this._relativeFilePath;
        public string GetRelativePath => this._relativePath;
        public string GetMD5Code => this._md5Code;

        public bool IsFileTheSame(FileProps comparedToFile)
        {
            return this.GetRelativeFilePath == comparedToFile.GetRelativeFilePath && IsSameMD5(this.GetMD5Code, comparedToFile.GetMD5Code);
        }

        public bool IsFileCopied(FileProps comparedToFile)
        {
            return (this.GetFileName == comparedToFile.GetFileName && IsSameMD5(this.GetMD5Code, comparedToFile.GetMD5Code)) && this.GetRelativePath != comparedToFile.GetRelativePath;
        }

        public bool IsFileModified(FileProps comparedToFile)
        {
            return this.GetRelativeFilePath == comparedToFile.GetRelativeFilePath && !IsSameMD5(this.GetMD5Code, comparedToFile.GetMD5Code);
        }
        
        private static bool IsSameMD5(string origin, string backup)
        {
            return String.Equals(origin, backup, StringComparison.OrdinalIgnoreCase);
        }
    }
}