using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ABC_Retail.Models;
using Microsoft.AspNetCore.Http;

namespace ABC_Retail.Services
{
    public interface IAzureBlobService
    {
        Task<UploadedFile> UploadFileAsync(IFormFile file, string containerName, string description = "", string category = "");
        Task<bool> DeleteFileAsync(string fileName, string containerName);
        Task<Stream> DownloadFileAsync(string fileName, string containerName);
        Task<List<UploadedFile>> ListFilesAsync(string containerName);
        Task<string> GetFileUrlAsync(string fileName, string containerName);
        Task<bool> ContainerExistsAsync(string containerName);
        Task<bool> CreateContainerAsync(string containerName);
    }

    public class AzureBlobService : IAzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public AzureBlobService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task<UploadedFile> UploadFileAsync(IFormFile file, string containerName, string description = "", string category = "")
        {
            try
            {
                // Ensure container exists
                await CreateContainerAsync(containerName);

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(file.FileName);

                // Upload the file
                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, true);

                // Set metadata
                var metadata = new Dictionary<string, string>
                {
                    { "Description", description },
                    { "Category", category },
                    { "UploadDate", DateTime.UtcNow.ToString("O") },
                    { "OriginalFileName", file.FileName }
                };

                await blobClient.SetMetadataAsync(metadata);

                // Get blob properties to get file size
                var properties = await blobClient.GetPropertiesAsync();

                return new UploadedFile
                {
                    FileName = file.FileName,
                    ContainerName = containerName,
                    BlobUrl = blobClient.Uri.ToString(),
                    FileSize = properties.Value.ContentLength,
                    ContentType = file.ContentType,
                    UploadDate = DateTime.UtcNow,
                    Description = description,
                    Category = category
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName, string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }

        public async Task<Stream> DownloadFileAsync(string fileName, string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                var response = await blobClient.DownloadAsync();
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                throw;
            }
        }

        public async Task<List<UploadedFile>> ListFilesAsync(string containerName)
        {
            var files = new List<UploadedFile>();

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var properties = await blobClient.GetPropertiesAsync();
                    var metadata = properties.Value.Metadata;

                    files.Add(new UploadedFile
                    {
                        FileName = blobItem.Name,
                        ContainerName = containerName,
                        BlobUrl = blobClient.Uri.ToString(),
                        FileSize = properties.Value.ContentLength,
                        ContentType = properties.Value.ContentType,
                        UploadDate = properties.Value.LastModified.DateTime,
                        Description = metadata.TryGetValue("Description", out var desc) ? desc : string.Empty,
                        Category = metadata.TryGetValue("Category", out var cat) ? cat : string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing files: {ex.Message}");
            }

            return files;
        }

        public async Task<string> GetFileUrlAsync(string fileName, string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting file URL: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> ContainerExistsAsync(string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                return await containerClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking container existence: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateContainerAsync(string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating container: {ex.Message}");
                return false;
            }
        }
    }
}
