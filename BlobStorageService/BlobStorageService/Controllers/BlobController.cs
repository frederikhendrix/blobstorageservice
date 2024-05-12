using BlobStorageService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BlobStorageService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BlobController : ControllerBase
    {
        private readonly IBlobService _blobService;

        public BlobController(IBlobService blobService)
        {
            _blobService = blobService;
        }

        [HttpGet]
        public IActionResult GetVideoMetadata()
        {
            // Generate SAS token for video blob
            var blobReadUrl = _blobService.GenerateBlobReadSasUri("flixblobstorage1", "brothers.mp4");

            // Return the video metadata with the SAS token-enabled URL
            return Ok(new { VideoUrl = blobReadUrl });
        }
    }
}
