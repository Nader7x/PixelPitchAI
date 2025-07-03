using Application.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Application.Services;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobConnection");
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string?> UploadImageAsync(IFormFile file, string containerName)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file was provided");

        // Create the container if it doesn't exist
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        // Create a unique file name
        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var blobClient = containerClient.GetBlobClient(fileName);

        // Upload the file
        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = file.ContentType }
        );

        // Return the URL to the blob
        return blobClient.Uri.ToString();
    }

    public async Task DeleteImageAsync(string imageUrl, string containerName)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Extract the blob name from the URL
            var uri = new Uri(imageUrl);
            var blobName = Path.GetFileName(uri.LocalPath);

            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
        catch (Exception)
        {
            // Log the exception but don't rethrow
        }
    }
}
