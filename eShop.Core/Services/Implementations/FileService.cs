using Microsoft.Extensions.Configuration;
using eShop.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;

namespace eShop.Core.Services.Implementations
{
    public class FileService(IConfiguration configuration) : IFileService
    {
        private readonly string _uploadsPath = configuration["FileStorage:UploadsPath"]
                ?? throw new ArgumentNullException("FileStorage:UploadsPath not configured in appsettings.json");

        public async Task<string> SaveFileAsync(IFormFile file, string subfolder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file provided or file is empty.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif",".webp",".avif", ".jfif" };
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

            // --- 1. Define the web root part to remove (domain, port, and scheme) ---
            // This looks for the first occurrence of '/Uploads/' to start the relative path.
            const string WebRootIdentifier = "/Uploads/";

            var relativePath = filePath;

            // --- 2. Check if it's a full URL and extract the relevant part ---
            var startIndex = filePath.IndexOf(WebRootIdentifier, StringComparison.OrdinalIgnoreCase);

            if (startIndex >= 0)
            {
                // Extract the path starting from 'Uploads/...'
                relativePath = filePath.Substring(startIndex + 1); // +1 to keep the leading slash on 'Uploads'
            }

            // --- 3. Clean up the relative path (remove 'Uploads/' if _uploadsPath already points to it) ---
            // The physical _uploadsPath likely points *to* the Uploads folder.
            // If _uploadsPath is C:\App\wwwroot\Uploads, then we need the path *after* Uploads/.
            // The relative path is now: Uploads/general/27c698b6-...png

            const string BaseFolder = "Uploads/";
            if (relativePath.StartsWith(BaseFolder, StringComparison.OrdinalIgnoreCase))
            {
                // Remove the 'Uploads/' part so the path starts at 'general/...'
                relativePath = relativePath.Substring(BaseFolder.Length);
            }

            // --- 4. Combine the physical base path with the corrected relative path ---
            // relativePath is now: general/27c698b6-...png
            // Path.Combine will handle the operating system separators.
            var fullPath = Path.Combine(_uploadsPath, relativePath);

            if (File.Exists(fullPath))
            {
                // Now it should find the file: C:\App\wwwroot\Uploads\general\27c698b6-....png
                await Task.Run(() => File.Delete(fullPath));
            }
        }
    }
}