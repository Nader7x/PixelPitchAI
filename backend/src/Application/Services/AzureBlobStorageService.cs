using Application.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Application.Services;

public class AzureBlobStorageService : IFileStorageService, ISpatialTrackStorageService
{
    private const string SpatialTracksContainerName = "spatial-tracks";
    private readonly BlobServiceClient? _blobServiceClient;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                _blobServiceClient = new BlobServiceClient(connectionString);
            }
            catch
            {
                _blobServiceClient = null;
            }
        }
    }

    public AzureBlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    async Task<string?> IFileStorageService.UploadImageAsync(IFormFile file, string containerName)
    {
        if (_blobServiceClient == null)
            throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

        if (file == null || file.Length == 0)
            throw new ArgumentException("No file was provided");

        // Create the container if it doesn't exist
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        // Create a unique file name
        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var blobClient = containerClient.GetBlobClient(fileName);

        // Upload the file
        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = file.ContentType }
        );

        // Return the URL to the blob
        return blobClient.Uri.ToString();
    }

    public async Task DeleteImageAsync(string? imageUrl, string containerName)
    {
        if (string.IsNullOrEmpty(imageUrl) || _blobServiceClient == null)
            return;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Extract the blob name from the URL
            var uri = new Uri(imageUrl);
            var blobName = Path.GetFileName(uri.LocalPath);

            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
        catch (Exception)
        {
            // Log the exception but don't rethrow
        }
    }

    public async Task<string> UploadSpatialTrackAsync(
        string objectKey,
        Stream zstdStream,
        CancellationToken cancellationToken = default
    )
    {
        if (_blobServiceClient == null)
            throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentNullException.ThrowIfNull(zstdStream);

        var containerClient = _blobServiceClient.GetBlobContainerClient(SpatialTracksContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(objectKey);
        await blobClient.UploadAsync(
            zstdStream,
            new BlobHttpHeaders { ContentType = "application/zstd" },
            cancellationToken: cancellationToken
        );

        return objectKey;
    }

    public async Task<Stream?> OpenSpatialTrackReadStreamAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        if (_blobServiceClient == null || string.IsNullOrWhiteSpace(objectKey))
            return null;

        var containerClient = _blobServiceClient.GetBlobContainerClient(SpatialTracksContainerName);
        var blobClient = containerClient.GetBlobClient(objectKey);
        if (!await blobClient.ExistsAsync(cancellationToken))
            return null;

        return await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }

    public async Task DeleteSpatialTrackAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        if (_blobServiceClient == null || string.IsNullOrWhiteSpace(objectKey))
            return;

        var containerClient = _blobServiceClient.GetBlobContainerClient(SpatialTracksContainerName);
        var blobClient = containerClient.GetBlobClient(objectKey);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
    }
}
