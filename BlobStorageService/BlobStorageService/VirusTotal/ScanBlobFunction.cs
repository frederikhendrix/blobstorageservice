using Microsoft.Azure.WebJobs;
using VirusTotalNet.Results;


namespace BlobStorageService.VirusTotal
{
    public static class ScanBlobFunction
    {
        [FunctionName("ScanBlobFunction")]
        public static async Task Run(
            [BlobTrigger("uploads/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
            string name,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blobStream.Length} Bytes");

            // Read the blob content
            using (var memoryStream = new MemoryStream())
            {
                await blobStream.CopyToAsync(memoryStream);
                byte[] fileBytes = memoryStream.ToArray();

                // Scan file using VirusTotal
                var result = await ScanFileWithVirusTotal(fileBytes, name, log);
                if (result)
                {
                    log.LogInformation($"File {name} is clean.");
                }
                else
                {
                    log.LogWarning($"File {name} is infected and has been quarantined.");
                    // Handle the infected file (e.g., move to quarantine container)
                    await MoveToQuarantineContainer(name, fileBytes, log);
                }
            }
        }

        private static async Task<bool> ScanFileWithVirusTotal(byte[] fileBytes, string fileName, ILogger log)
        {
            var apiKey = Environment.GetEnvironmentVariable("VIRUSTOTAL_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                log.LogError("VirusTotal API key is missing.");
                return false;
            }

            var virusTotal = new VirusTotalNet.VirusTotal(apiKey);
            virusTotal.UseTLS = true;

            // Check if the file has already been scanned
            FileReport fileReport = await virusTotal.GetFileReportAsync(fileBytes);
            if (fileReport.ResponseCode == VirusTotalNet.ResponseCodes.FileReportResponseCode.Present)
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
                    if (fileReport.ResponseCode == VirusTotalNet.ResponseCodes.FileReportResponseCode.Present)
                    {
                        return fileReport.Positives == 0;
                    }
                    await Task.Delay(10000); // Wait for 10 seconds before polling again
                }
            }
        }

        private static async Task MoveToQuarantineContainer(string fileName, byte[] fileBytes, ILogger log)
        {
            var connectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                log.LogError("Azure Blob Storage connection string is missing.");
                return;
            }

            var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(connectionString);
            var quarantineContainerClient = blobServiceClient.GetBlobContainerClient("quarantine");

            // Ensure the quarantine container exists
            await quarantineContainerClient.CreateIfNotExistsAsync();

            var quarantineBlobClient = quarantineContainerClient.GetBlobClient(fileName);
            using (var memoryStream = new MemoryStream(fileBytes))
            {
                await quarantineBlobClient.UploadAsync(memoryStream, true);
            }

            log.LogInformation($"File {fileName} moved to quarantine container.");
        }

    }
}
