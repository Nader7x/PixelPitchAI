using System;
using System.IO;
using System.Threading.Tasks;
using Application.Services;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Footex.UnitTests.Services
{
    public class AzureBlobStorageServiceTests
    {
        private readonly Mock<BlobServiceClient> _blobServiceClientMock;
        private readonly Mock<BlobContainerClient> _blobContainerClientMock;
        private readonly AzureBlobStorageService _storageService;

        public AzureBlobStorageServiceTests()
        {
            _blobServiceClientMock = new Mock<BlobServiceClient>();
            _blobContainerClientMock = new Mock<BlobContainerClient>();

            _storageService = new AzureBlobStorageService(_blobServiceClientMock.Object);
        }

        [Fact]
        public async Task UploadImageAsync_WithValidFile_ShouldReturnUrl()
        {
            var fileMock = new Mock<IFormFile>();
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("test content");
            writer.Flush();
            stream.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            fileMock.Setup(f => f.FileName).Returns("test.jpg");
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

            var containerName = "test-container";
            var blobName = "test.jpg";
            var blobUri = new Uri(
                $"http://127.0.0.1:10000/devstoreaccount1/{containerName}/{blobName}"
            );

            var blobClientMock = new Mock<BlobClient>();

            _blobServiceClientMock
                .Setup(s => s.GetBlobContainerClient(containerName))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(c => c.CreateIfNotExistsAsync(PublicAccessType.Blob, null, default, default))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            _blobContainerClientMock
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);

            blobClientMock.Setup(b => b.Uri).Returns(blobUri);

            blobClientMock
                .Setup(b =>
                    b.UploadAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<BlobHttpHeaders>(),
                        null,
                        null,
                        null,
                        null,
                        default,
                        default
                    )
                )
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var result = await _storageService.UploadImageAsync(fileMock.Object, containerName);

            Assert.Equal(blobUri.ToString(), result);
        }

        [Fact]
        public async Task DeleteImageAsync_WithValidUrl_ShouldDeleteBlob()
        {
            var imageUrl = "http://127.0.0.1:10000/devstoreaccount1/test-container/test.jpg";
            var containerName = "test-container";
            var blobName = "test.jpg";
            var blobUri = new Uri(imageUrl);

            var blobClientMock = new Mock<BlobClient>();

            _blobServiceClientMock
                .Setup(s => s.GetBlobContainerClient(containerName))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(c => c.GetBlobClient(blobName))
                .Returns(blobClientMock.Object);

            blobClientMock
                .Setup(b =>
                    b.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, default)
                )
                .ReturnsAsync(Response.FromValue(true, new Mock<Response>().Object));

            await _storageService.DeleteImageAsync(imageUrl, containerName);

            blobClientMock.Verify(
                b => b.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, default),
                Times.Once
            );
        }
    }
}
