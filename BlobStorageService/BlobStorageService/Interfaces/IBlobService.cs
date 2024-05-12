namespace BlobStorageService.Interfaces
{
    public interface IBlobService
    {
        string GenerateBlobReadSasUri(string containerName, string blobName);
    }
}
