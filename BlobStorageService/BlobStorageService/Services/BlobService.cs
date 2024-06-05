using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using BlobStorageService.Interfaces;
using Microsoft.WindowsAzure.Storage;
using VirusTotalNet;
using VirusTotalNet.ResponseCodes;
using VirusTotalNet.Results;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BlobStorageService.Services
{
    public class BlobService : IBlobService
    {
        private readonly string _storageConnectionString;
        private readonly string _virusTotalApiKey;
        private readonly ILogger<BlobService> _logger;

        public BlobService(ILogger<BlobService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _storageConnectionString = configuration["BlobStorage:ConnectionString"];
            _virusTotalApiKey = configuration["BlobStorage:VirusTotalApiKey"];

            // Debugging logs
            Console.WriteLine($"BlobStorage ConnectionString: {_storageConnectionString}");
            Console.WriteLine($"VirusTotal ApiKey: {_virusTotalApiKey}");
        }

        public string GenerateBlobReadSasUri(string containerName, string blobName)
        {
            // Parse the connection string
            var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            var blobServiceClient = new BlobServiceClient(_storageConnectionString);
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

        public async Task<string> UploadBlobAsync(string containerName, string blobName, Stream content)
        {
            // Save the file to a temporary location
            var tempFilePath = Path.GetTempFileName();
            using (var fileStream = File.Create(tempFilePath))
            {
                await content.CopyToAsync(fileStream);
            }

            // Scan the file with VirusTotal
            if (!await ScanFileWithVirusTotal(tempFilePath, blobName))
            {
                File.Delete(tempFilePath);
                throw new Exception("File is infected with a virus.");
            }

            var blobServiceClient = new BlobServiceClient(_storageConnectionString);
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

            using (var fileStream = File.OpenRead(tempFilePath))
            {
                await blobClient.UploadAsync(fileStream, true);
            }

            File.Delete(tempFilePath);

            return uniqueBlobName;
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var blobServiceClient = new BlobServiceClient(_storageConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
        }

        private async Task<bool> ScanFileWithVirusTotal(string filePath, string fileName)
        {
            var apiKey =  _virusTotalApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("VirusTotal API key is missing.");
                return false;
            }

            var virusTotal = new VirusTotalNet.VirusTotal(apiKey);
            virusTotal.UseTLS = true;

            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

            // Check if the file has already been scanned
            FileReport fileReport = await virusTotal.GetFileReportAsync(fileBytes);
            if (fileReport.ResponseCode == FileReportResponseCode.Present)
            {
                // File has already been scanned, check the result
                return fileReport.Positives == 0;
            }
            else
            {
                // File has not been scanned, upload and scan
                ScanResult scanResult = await virusTotal.ScanFileAsync(fileBytes, fileName);

                // Poll the report
                while (true)
                {
                    fileReport = await virusTotal.GetFileReportAsync(scanResult.Resource);
                    if (fileReport.ResponseCode == FileReportResponseCode.Present)
                    {
                        return fileReport.Positives == 0;
                    }
                    await Task.Delay(10000); // Wait for 10 seconds before polling again
                }
            }
        }
    }
}
