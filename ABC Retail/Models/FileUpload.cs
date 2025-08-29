using Microsoft.AspNetCore.Http;

namespace ABC_Retail.Models
{
    public class FileUpload
    {
        public IFormFile File { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class UploadedFile
    {
        public string FileName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string BlobUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class LogFile
    {
        public string FileName { get; set; } = string.Empty;
        public string ShareName { get; set; } = string.Empty;
        public string DirectoryPath { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
