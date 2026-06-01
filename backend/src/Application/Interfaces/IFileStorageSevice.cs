using Microsoft.AspNetCore.Http;

namespace Application.Interfaces;

public interface IFileStorageService
{
    Task<string?> UploadImageAsync(IFormFile file, string containerName);
    Task DeleteImageAsync(string? imageUrl, string containerName);
}
