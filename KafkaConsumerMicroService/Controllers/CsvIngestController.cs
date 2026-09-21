using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using KafkaConsumerMicroService.Data;
using KafkaConsumerMicroService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KafkaConsumerMicroService.Controllers
{
    [ApiController]
    [Route("api/ingest")]
    public class CsvIngestController : ControllerBase
    {
        private readonly IFileImportService _importService;

        public CsvIngestController(IFileImportService importService)
        {
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        }

        // POST api/ingest/csv
        // Expects multipart/form-data with file field named 'file'
        [HttpPost("csv")]
        [RequestSizeLimit(50_000_000)] // 50 MB for demo; configure appropriately
        public async Task<IActionResult> UploadCsv([FromForm(Name = "file")] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            using var stream = file.OpenReadStream();
            var (processed, errors) = await _importService.ImportCsvAsync(stream, HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(new { processed, errors });
        }
    }
}
