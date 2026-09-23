using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KafkaConsumerMicroService.Data;

namespace KafkaConsumerMicroService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HealthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow });

        [HttpGet("valid-records")]
        public async Task<IActionResult> GetValidRecords(CancellationToken cancellationToken)
        {
            // Use no-tracking and projection to DTO for faster, lighter queries
            var list = await _db.MarketRecords
                .AsNoTracking()
                .Where(r => r.IsValid)
                .OrderByDescending(r => r.ReceivedAt)
                .Select(r => new KafkaConsumerMicroService.Dtos.MarketRecordDto
                {
                    Id = r.Id,
                    SourceSystem = r.SourceSystem,
                    AccountNumber = r.AccountNumber,
                    PnLAmount = r.PnLAmount,
                    IsValid = r.IsValid,
                    ReceivedAt = r.ReceivedAt,
                    Metadata = r.Metadata
                })
                .Take(200)
                .ToListAsync(cancellationToken);

            return Ok(list);
        }

        [HttpGet("invalid-records")]
        public async Task<IActionResult> GetInvalidRecords(CancellationToken cancellationToken)
        {
            var list = await _db.MarketRecords
                .AsNoTracking()
                .Where(r => !r.IsValid)
                .OrderByDescending(r => r.ReceivedAt)
                .Select(r => new KafkaConsumerMicroService.Dtos.MarketRecordDto
                {
                    Id = r.Id,
                    SourceSystem = r.SourceSystem,
                    AccountNumber = r.AccountNumber,
                    PnLAmount = r.PnLAmount,
                    IsValid = r.IsValid,
                    ReceivedAt = r.ReceivedAt,
                    Metadata = r.Metadata
                })
                .Take(200)
                .ToListAsync(cancellationToken);

            return Ok(list);
        }
    }
}
