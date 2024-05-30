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

        /**
         
        returns unique blob name if needed. 
         */
        public async Task<string> UploadBlobAsync(string containerName, string blobName, Stream content)
        {
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            var uniqueBlobName = blobName;

            // Check if the blob already exists
            if (await blobClient.ExistsAsync())
            {
                // Generate a new unique blob name
                uniqueBlobName = $"{Path.GetFileNameWithoutExtension(blobName)}_{Guid.NewGuid()}{Path.GetExtension(blobName)}";
                blobClient = blobContainerClient.GetBlobClient(uniqueBlobName);
            }

            await blobClient.UploadAsync(content, true);
            return uniqueBlobName;
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
        }
    }
}
