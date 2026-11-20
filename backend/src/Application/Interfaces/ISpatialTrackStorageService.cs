namespace Application.Interfaces;

/// <summary>
///     Offloads Zstd-compressed 22-player spatial track blobs (~350-500 KB/match)
///     to Cloudflare R2 (primary) or Azure Blob Storage (fallback).
/// </summary>
public interface ISpatialTrackStorageService
{
    Task<string> UploadSpatialTrackAsync(
        string objectKey,
        Stream zstdStream,
        CancellationToken cancellationToken = default
    );

    Task<Stream?> OpenSpatialTrackReadStreamAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    );

    Task DeleteSpatialTrackAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    );
}
