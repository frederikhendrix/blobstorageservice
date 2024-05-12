using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using BlobStorageService.Interfaces;
using Microsoft.WindowsAzure.Storage;

namespace BlobStorageService.Services
{
    public class BlobService : IBlobService
    {
        private readonly string storageConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING");

        public string GenerateBlobReadSasUri(string containerName, string blobName)
        {
            // Parse the connection string
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            // Set the expiry time and permissions for the SAS
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Specify read permissions for the SAS
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Get account name and key from the connection string
            string accountName = storageAccount.Credentials.AccountName;
            string accountKey = storageAccount.Credentials.ExportBase64EncodedKey();

            // Generate the SAS token
            var sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(accountName, accountKey)).ToString();

            // Return the full URI with the SAS token
            string result = $"{blobClient.Uri}?{sasToken}";
            return result;
        }
    }
}
