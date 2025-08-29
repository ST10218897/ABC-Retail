using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using ABC_Retail.Models;

namespace ABC_Retail.Services
{
    public interface IAzureFileService
    {
        Task<bool> UploadLogFileAsync(string fileName, string content, string shareName = "logs");
        Task<string?> DownloadLogFileAsync(string fileName, string shareName = "logs");
        Task<List<LogFile>> ListLogFilesAsync(string shareName = "logs");
        Task<bool> DeleteLogFileAsync(string fileName, string shareName = "logs");
        Task<bool> ShareExistsAsync(string shareName);
        Task<bool> CreateShareAsync(string shareName);
    }

    public class AzureFileService : IAzureFileService
    {
        private readonly ShareServiceClient _shareServiceClient;

        public AzureFileService(ShareServiceClient shareServiceClient)
        {
            _shareServiceClient = shareServiceClient;
        }

        public async Task<bool> UploadLogFileAsync(string fileName, string content, string shareName = "logs")
        {
            try
            {
                // Ensure share exists
                await CreateShareAsync(shareName);

                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                // Convert content to bytes
                byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(content);

                using (var stream = new MemoryStream(contentBytes))
                {
                    await fileClient.CreateAsync(stream.Length);
                    await fileClient.UploadRangeAsync(
                        new HttpRange(0, stream.Length),
                        stream);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading log file: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> DownloadLogFileAsync(string fileName, string shareName = "logs")
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                if (!await fileClient.ExistsAsync())
                    return null;

                var response = await fileClient.DownloadAsync();
                using (var streamReader = new StreamReader(response.Value.Content))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading log file: {ex.Message}");
                return null;
            }
        }

        public async Task<List<LogFile>> ListLogFilesAsync(string shareName = "logs")
        {
            var logFiles = new List<LogFile>();

            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                
                if (!await shareClient.ExistsAsync())
                    return logFiles;

                var directoryClient = shareClient.GetRootDirectoryClient();

                await foreach (var fileItem in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!fileItem.IsDirectory)
                    {
                        var fileClient = directoryClient.GetFileClient(fileItem.Name);
                        var properties = await fileClient.GetPropertiesAsync();

                        // Download file content
                        string content = await DownloadLogFileAsync(fileItem.Name, shareName) ?? string.Empty;

                        logFiles.Add(new LogFile
                        {
                            FileName = fileItem.Name,
                            ShareName = shareName,
                            DirectoryPath = "/",
                            FilePath = $"/{fileItem.Name}",
                            FileSize = properties.Value.ContentLength,
                            LastModified = properties.Value.LastModified.DateTime,
                            Content = content
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing log files: {ex.Message}");
            }

            return logFiles;
        }

        public async Task<bool> DeleteLogFileAsync(string fileName, string shareName = "logs")
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                return await fileClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting log file: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ShareExistsAsync(string shareName)
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                return await shareClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking share existence: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateShareAsync(string shareName)
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                await shareClient.CreateIfNotExistsAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating share: {ex.Message}");
                return false;
            }
        }
    }
}
