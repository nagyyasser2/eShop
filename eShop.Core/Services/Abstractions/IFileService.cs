using Microsoft.AspNetCore.Http;

namespace eShop.Core.Services.Abstractions
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string subfolder);
        Task<List<string>> SaveFilesAsync(IList<IFormFile> files, string subfolder);
        Task DeleteFileAsync(string filePath);
    }
}
