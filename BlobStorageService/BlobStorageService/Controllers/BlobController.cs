using BlobStorageService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BlobStorageService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BlobController : ControllerBase
    {
        private readonly IBlobService _blobService;

        public BlobController(IBlobService blobService)
        {
            _blobService = blobService;
        }

        [HttpGet("{name}")]
        public IActionResult GetVideoMetadata(string name)
        {
            // Generate SAS token for video blob
            var blobReadUrl = _blobService.GenerateBlobReadSasUri("flixblobstorage1", name);

            // Return the video metadata with the SAS token-enabled URL
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
                var blobUniqueName = await _blobService.UploadBlobAsync("flixblobstorage1", blobName, stream);
                return Ok(new { BlobName = blobUniqueName });
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

    }
}
