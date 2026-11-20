using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
///     Primary Cloudflare R2 (S3-compatible) spatial track storage service with
///     automatic fallback to <see cref="AzureBlobStorageService"/> when R2 is not configured.
///     Uses reflection-free AWS SigV4 signing for Native AOT compatibility.
/// </summary>
public sealed class CloudflareR2SpatialTrackStorageService : ISpatialTrackStorageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AzureBlobStorageService _azureFallback;
    private readonly ILogger<CloudflareR2SpatialTrackStorageService> _logger;
    private readonly string? _endpoint;
    private readonly string? _bucket;
    private readonly string? _accessKeyId;
    private readonly string? _secretAccessKey;
    private readonly bool _isR2Configured;

    public CloudflareR2SpatialTrackStorageService(
        IHttpClientFactory httpClientFactory,
        AzureBlobStorageService azureFallback,
        IConfiguration configuration,
        ILogger<CloudflareR2SpatialTrackStorageService> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _azureFallback = azureFallback;
        _logger = logger;

        _endpoint = (configuration["R2:Endpoint"] ?? Environment.GetEnvironmentVariable("R2_ENDPOINT"))?.TrimEnd('/');
        _bucket = configuration["R2:BucketName"] ?? Environment.GetEnvironmentVariable("R2_BUCKET_NAME") ?? "pixelpitch-spatial-tracks";
        _accessKeyId = configuration["R2:AccessKeyId"] ?? Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID");
        _secretAccessKey = configuration["R2:SecretAccessKey"] ?? Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY");

        _isR2Configured =
            !string.IsNullOrWhiteSpace(_endpoint)
            && !string.IsNullOrWhiteSpace(_bucket)
            && !string.IsNullOrWhiteSpace(_accessKeyId)
            && !string.IsNullOrWhiteSpace(_secretAccessKey);
    }

    public async Task<string> UploadSpatialTrackAsync(
        string objectKey,
        Stream zstdStream,
        CancellationToken cancellationToken = default
    )
    {
        if (!_isR2Configured)
        {
            _logger.LogDebug("Cloudflare R2 not configured; falling back to Azure Blob Storage for {ObjectKey}", objectKey);
            return await _azureFallback.UploadSpatialTrackAsync(objectKey, zstdStream, cancellationToken);
        }

        using var ms = new MemoryStream();
        await zstdStream.CopyToAsync(ms, cancellationToken);
        var payload = ms.ToArray();

        var uri = BuildObjectUri(objectKey);
        using var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new ByteArrayContent(payload),
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zstd");
        SignRequest(request, payload);

        var client = _httpClientFactory.CreateClient("R2SpatialStorage");
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return objectKey;
    }

    public async Task<Stream?> OpenSpatialTrackReadStreamAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        if (!_isR2Configured)
            return await _azureFallback.OpenSpatialTrackReadStreamAsync(objectKey, cancellationToken);

        var uri = BuildObjectUri(objectKey);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        SignRequest(request, []);

        var client = _httpClientFactory.CreateClient("R2SpatialStorage");
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task DeleteSpatialTrackAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        if (!_isR2Configured)
        {
            await _azureFallback.DeleteSpatialTrackAsync(objectKey, cancellationToken);
            return;
        }

        var uri = BuildObjectUri(objectKey);
        using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        SignRequest(request, []);

        var client = _httpClientFactory.CreateClient("R2SpatialStorage");
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }

    private Uri BuildObjectUri(string objectKey)
    {
        var normalizedKey = objectKey.TrimStart('/');
        return new Uri($"{_endpoint}/{_bucket}/{normalizedKey}");
    }

    private void SignRequest(HttpRequestMessage request, byte[] payload)
    {
        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var payloadHash = Convert.ToHexStringLower(SHA256.HashData(payload));

        var uri = request.RequestUri!;
        request.Headers.Host = uri.Authority;
        request.Headers.Add("x-amz-date", amzDate);
        request.Headers.Add("x-amz-content-sha256", payloadHash);

        var canonicalHeaders =
            $"host:{uri.Authority}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n";
        const string signedHeaders = "host;x-amz-content-sha256;x-amz-date";

        var canonicalRequest =
            $"{request.Method.Method}\n{uri.AbsolutePath}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        var canonicalHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));

        const string region = "auto";
        const string service = "s3";
        var scope = $"{dateStamp}/{region}/{service}/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{scope}\n{canonicalHash}";

        var kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + _secretAccessKey), dateStamp);
        var kRegion = HmacSha256(kDate, region);
        var kService = HmacSha256(kRegion, service);
        var kSigning = HmacSha256(kService, "aws4_request");
        var signature = Convert.ToHexStringLower(HmacSha256(kSigning, stringToSign));

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            $"AWS4-HMAC-SHA256 Credential={_accessKeyId}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}"
        );
    }

    private static byte[] HmacSha256(byte[] key, string data) =>
        HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));
}
