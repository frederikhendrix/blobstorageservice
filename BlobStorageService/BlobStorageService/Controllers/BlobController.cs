using BlobStorageService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BlobStorageService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BlobController : ControllerBase
    {
        private readonly IBlobService _blobService;
        private readonly ILogger<BlobController> _logger;
        private const string EncryptionKey = "bXlzZWNyZXRrZXkxMjM0NQ=="; 
        private const string Iv = "bXlzZWNyZXRpdjEyMzQ1Ng==";

        public BlobController(IBlobService blobService, ILogger<BlobController> logger)
        {
            _blobService = blobService;
            _logger = logger;
        }

        [HttpGet("{name}")]
        public IActionResult GetVideoMetadata(string name)
        {

            //var decryptedName = Decrypt(name);
            // Generate SAS token for video blob
            var blobReadUrl = _blobService.GenerateBlobReadSasUri("flixblobstorage1", name);
            var encryptedUrl = Encrypt(blobReadUrl);
            // Return the video metadata with the SAS token-enabled URL.
            return Ok(new { VideoUrl = blobReadUrl }); 
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideo([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a valid MP4 file.");

            if (file.ContentType != "video/mp4")
                return BadRequest("Please upload a valid MP4 file.");

            var blobName = file.FileName;
            using (var stream = file.OpenReadStream())
            {
                try
                {
                    var blobUniqueName = await _blobService.UploadBlobAsync("flixblobstorage1", blobName, stream);
                    return Ok(new { BlobName = blobUniqueName });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file");
                    return StatusCode(500, "Internal server error");
                }
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteBlob([FromBody] DeleteBlobRequest request)
        {
            await _blobService.DeleteBlobAsync(request.ContainerName, request.BlobName);
            return NoContent();
        }

        public class DeleteBlobRequest
        {
            public string ContainerName { get; set; }
            public string BlobName { get; set; }
        }

        private string Decrypt(string encryptedText)
        {
            var key = Convert.FromBase64String(EncryptionKey);
            var iv = Convert.FromBase64String(Iv);

            using (var aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                var buffer = Convert.FromBase64String(encryptedText);

                using (var msDecrypt = new MemoryStream(buffer))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        private string Encrypt(string plainText)
        {
            var key = Convert.FromBase64String(EncryptionKey);
            var iv = Convert.FromBase64String(Iv);

            using (var aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (var msEncrypt = new MemoryStream())
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                    swEncrypt.Close();
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
    }
}
