using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;

namespace HackerRank.Services
{
    public class FileStorageService
    {
        private readonly string _uploadDirectory;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _baseUrl;

        public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
        {
            _logger = logger;

            // Get base directory from configuration:
            _uploadDirectory = string.IsNullOrWhiteSpace(configuration["FileStorage:UploadDirectory"])
     ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images")
     : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuration["FileStorage:UploadDirectory"].TrimStart('/')));

            // Get base URL from configuration
            _baseUrl = configuration["FileStorage:BaseUrl"]
                ?? throw new ArgumentNullException("FileStorage:BaseUrl configuration is missing");

            // Ensure upload directories exist
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
                _logger.LogInformation($"Created directory: {_uploadDirectory}");
            }
        }

        public async Task<string> UploadFile(IFormFile file, string folderPath = "", int maxWidth = 800, int maxHeight = 800)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Attempted to upload null or empty file");
                    return string.Empty;
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    _logger.LogWarning($"Invalid file type: {file.ContentType}");
                    throw new ArgumentException("Invalid file type. Only JPEG, PNG, and GIF are allowed.");
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                // Ensure folderPath is properly appended
                var directory = string.IsNullOrWhiteSpace(folderPath)
                    ? _uploadDirectory
                    : Path.Combine(_uploadDirectory, folderPath);

                var filePath = Path.Combine(directory, fileName);

                // Create directory if it doesn't exist
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Resize and save the image
                using (var stream = file.OpenReadStream())   
                {
                    if (stream == null || !stream.CanRead)
                    {
                        throw new InvalidOperationException("The file stream is not readable.");
                    }

                    using (var image = await Image.LoadAsync(stream))
                    {
                        // Validate image dimensions
                        if (image.Width == 0 || image.Height == 0)
                        {
                            throw new InvalidOperationException("Invalid image dimensions.");
                        }

                        // Resize the image while maintaining aspect ratio
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxWidth, maxHeight)
                        }));

                        // Save the resized image
                        await image.SaveAsync(filePath);
                    }
                }

                _logger.LogInformation($"Successfully uploaded and resized file: {filePath}");

                // Return relative URL for the file
                return $"/images/{folderPath}/{fileName}".Replace("//", "/"); // Ensure proper URL format
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                throw;
            }
        }

        public async Task DeleteFile(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                {
                    _logger.LogWarning("File URL is null or empty. Skipping deletion.");
                    return;
                }

                // Handle relative URL
                var relativePath = fileUrl.TrimStart('/').Replace("images/", "");
                var fullPath = Path.Combine(_uploadDirectory, relativePath); // Append relativePath to _uploadDirectory

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"Successfully deleted file: {fullPath}");
                }
                else
                {
                    _logger.LogWarning($"File not found for deletion: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {fileUrl}");
                throw;
            }
        }
    }
}
