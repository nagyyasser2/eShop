using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using eShop.Core.Services.Abstractions;

namespace eShop.Core.Services.Implementations
{
    public class FileService : IFileService
    {
        private readonly string _uploadsPath;

        public FileService(IConfiguration configuration)
        {
            _uploadsPath = configuration["FileStorage:UploadsPath"]
                ?? throw new ArgumentNullException("FileStorage:UploadsPath not configured in appsettings.json");
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subfolder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file provided or file is empty.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid file type. Only JPG, JPEG, PNG, and GIF are allowed.");
            }

            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                throw new ArgumentException("File size exceeds the maximum limit of 5MB.");
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_uploadsPath, subfolder);
            var filePath = Path.Combine(uploadsFolder, fileName);

            Directory.CreateDirectory(uploadsFolder);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"Uploads/{subfolder}/{fileName}";
        }

        public async Task<List<string>> SaveFilesAsync(IList<IFormFile> files, string subfolder)
        {
            if (files == null || !files.Any())
            {
                return new List<string>();
            }

            var filePaths = new List<string>();
            foreach (var file in files)
            {
                var filePath = await SaveFileAsync(file, subfolder);
                filePaths.Add(filePath);
            }

            return filePaths;
        }

        public async Task DeleteFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var fullPath = Path.Combine(_uploadsPath, filePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
            }
        }
    }
}