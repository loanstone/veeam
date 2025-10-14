namespace FolderSync
{
    public class Config
    {
        public int SyncPeriod { get; set; }
        public string SourceFolder { get; set; }
        public string BackupFolder { get; set; }
        public string LogFilePath { get; set; }
    }
}