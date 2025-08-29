using ABC_Retail.Models;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class FileController : Controller
    {
        private readonly IAzureFileService _fileService;
        private readonly IAzureBlobService _blobService;
        private readonly ILogger<FileController> _logger;

        public FileController(IAzureFileService fileService, IAzureBlobService blobService, ILogger<FileController> logger)
        {
            _fileService = fileService;
            _blobService = blobService;
            _logger = logger;
        }

        // GET: File
        public async Task<IActionResult> Index()
        {
            var logFiles = await _fileService.ListLogFilesAsync();
            var fileNames = logFiles.Select(log => log.FileName).ToList();
            return View(fileNames);
        }

        // GET: File/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // POST: File/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload([Bind("File,FileName,ContainerName,Description,Category")] FileUpload fileUpload)
        {
            if (ModelState.IsValid && fileUpload.File != null && fileUpload.File.Length > 0)
            {
                try
                {
                    var uploadedFile = await _blobService.UploadFileAsync(
                        fileUpload.File,
                        fileUpload.ContainerName,
                        fileUpload.Description,
                        fileUpload.Category
                    );

                    // Log the file upload
                    var logMessage = $"File uploaded: {uploadedFile.FileName}, Size: {uploadedFile.FileSize} bytes, Container: {uploadedFile.ContainerName}";
                    await _fileService.UploadLogFileAsync(
                        $"upload_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log",
                        logMessage
                    );

                    ViewBag.SuccessMessage = $"File '{uploadedFile.FileName}' uploaded successfully!";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file");
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("", "Please select a valid file to upload.");
            }

            return View(fileUpload);
        }

        // GET: File/DownloadLog/{fileName}
        public async Task<IActionResult> DownloadLog(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var content = await _fileService.DownloadLogFileAsync(fileName);
            if (content == null)
            {
                return NotFound();
            }

            return File(System.Text.Encoding.UTF8.GetBytes(content), "text/plain", fileName);
        }

        // GET: File/ViewLog/{fileName}
        public async Task<IActionResult> ViewLog(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var content = await _fileService.DownloadLogFileAsync(fileName);
            if (content == null)
            {
                return NotFound();
            }

            ViewBag.FileName = fileName;
            ViewBag.Content = content;

            return View();
        }

        // POST: File/DeleteLog/{fileName}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLog(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var success = await _fileService.DeleteLogFileAsync(fileName);
            if (success)
            {
                // Log the deletion
                var logMessage = $"File deleted: {fileName}, Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                await _fileService.UploadLogFileAsync(
                    $"delete_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log",
                    logMessage
                );

                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Failed to delete log file.");
            return RedirectToAction(nameof(Index));
        }

        // GET: File/BlobFiles
        public async Task<IActionResult> BlobFiles(string containerName = "product-images")
        {
            var files = await _blobService.ListFilesAsync(containerName);
            ViewBag.ContainerName = containerName;
            ViewBag.Containers = new List<string> { "product-images", "documents", "uploads" };
            
            return View(files);
        }

        // GET: File/DownloadBlob/{containerName}/{fileName}
        public async Task<IActionResult> DownloadBlob(string containerName, string fileName)
        {
            if (string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            try
            {
                var stream = await _blobService.DownloadFileAsync(fileName, containerName);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading blob file");
                ModelState.AddModelError("", $"Error downloading file: {ex.Message}");
                return RedirectToAction(nameof(BlobFiles));
            }
        }
    }
}
