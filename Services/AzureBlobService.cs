using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EventEaseApp.Services
{
    public class AzureBlobService : IBlobService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<AzureBlobService> _logger;
        private readonly string? _connectionString;
        private readonly string _containerName;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public AzureBlobService(
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILogger<AzureBlobService> logger)
        {
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;

            _connectionString = _configuration["AzureBlobStorage:ConnectionString"];
            _containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "eventease-images";
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file was uploaded or file is empty.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new ArgumentException($"File size exceeds maximum allowed limit of {MaxFileSizeBytes / (1024 * 1024)} MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Invalid file format '{extension}'. Only JPG, JPEG, PNG, WEBP, and GIF images are permitted.");
            }

            // Sanitize and generate unique file name
            var cleanFileName = Path.GetFileNameWithoutExtension(file.FileName)
                .Replace(" ", "_")
                .Replace("/", "")
                .Replace("\\", "");
            
            if (cleanFileName.Length > 30)
            {
                cleanFileName = cleanFileName.Substring(0, 30);
            }

            var uniqueFileName = $"{folderName}/{Guid.NewGuid():N}_{cleanFileName}{extension}";

            // Attempt Azure Blob Storage upload if connection string is configured
            if (!string.IsNullOrWhiteSpace(_connectionString) &&
                !_connectionString.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var blobServiceClient = new BlobServiceClient(_connectionString);
                    var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                    // Create container if it does not exist with public blob access
                    try
                    {
                        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not set PublicAccessType on container {ContainerName}; proceeding with existing permissions.", _containerName);
                        await containerClient.CreateIfNotExistsAsync();
                    }

                    var blobClient = containerClient.GetBlobClient(uniqueFileName);

                    using var stream = file.OpenReadStream();
                    var blobHttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    };

                    await blobClient.UploadAsync(stream, new BlobUploadOptions
                    {
                        HttpHeaders = blobHttpHeaders
                    });

                    _logger.LogInformation("Successfully uploaded image to Azure Blob Storage: {BlobUri}", blobClient.Uri);
                    return blobClient.Uri.ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Azure Blob Storage upload failed. Falling back to local storage.");
                }
            }
            else
            {
                _logger.LogInformation("Azure Blob Storage connection string not configured or empty. Using local fallback storage.");
            }

            // Local Fallback Storage (wwwroot/uploads/{folderName})
            return await SaveLocallyAsync(file, folderName, uniqueFileName);
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return false;

            if (!string.IsNullOrWhiteSpace(_connectionString) &&
                fileUrl.Contains(_containerName, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var blobServiceClient = new BlobServiceClient(_connectionString);
                    var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                    var uri = new Uri(fileUrl);
                    var blobName = uri.AbsolutePath.TrimStart('/');
                    // Remove container name prefix if included in path
                    if (blobName.StartsWith(_containerName + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        blobName = blobName.Substring(_containerName.Length + 1);
                    }

                    var blobClient = containerClient.GetBlobClient(blobName);
                    return await blobClient.DeleteIfExistsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete blob from Azure: {FileUrl}", fileUrl);
                }
            }

            // Local delete fallback
            if (fileUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
            }

            return false;
        }

        private async Task<string> SaveLocallyAsync(IFormFile file, string folderName, string uniqueFileName)
        {
            var uploadsRoot = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folderName);
            Directory.CreateDirectory(uploadsRoot);

            var fileNameOnly = Path.GetFileName(uniqueFileName);
            var filePath = Path.Combine(uploadsRoot, fileNameOnly);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folderName}/{fileNameOnly}";
        }
    }
}
