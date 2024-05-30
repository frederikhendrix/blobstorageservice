namespace BlobStorageService.Interfaces
{
    public interface IBlobService
    {
        string GenerateBlobReadSasUri(string containerName, string blobName);
        Task<string> UploadBlobAsync(string containerName, string blobName, Stream content);
        Task DeleteBlobAsync(string containerName, string blobName);


    }
}
