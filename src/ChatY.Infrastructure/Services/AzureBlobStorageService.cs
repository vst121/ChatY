using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace ChatY.Infrastructure.Services;

public interface IAzureBlobStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName, string? contentType = null);
    Task<bool> DeleteFileAsync(string fileName, string containerName);
    Task<Stream> DownloadFileAsync(string fileName, string containerName);
    Task<string> GetFileUrlAsync(string fileName, string containerName);
}

public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IConfiguration _configuration;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        var connectionString = configuration.GetConnectionString("AzureStorage");
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName, string? contentType = null)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType ?? "application/octet-stream"
            }
        };

        await blobClient.UploadAsync(fileStream, uploadOptions);
        return blobClient.Uri.ToString();
    }

    public async Task<bool> DeleteFileAsync(string fileName, string containerName)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        return await blobClient.DeleteIfExistsAsync();
    }

    public async Task<Stream> DownloadFileAsync(string fileName, string containerName)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        var response = await blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task<string> GetFileUrlAsync(string fileName, string containerName)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        return blobClient.Uri.ToString();
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        return containerClient;
    }
}


