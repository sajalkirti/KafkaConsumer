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
        public async Task<IActionResult> GetValidRecords()
        {
            var list = await _db.MarketRecords
                .Where(r => r.IsValid)
                .OrderByDescending(r => r.ReceivedAt)
                .Take(200)
                .ToListAsync();
            return Ok(list);
        }

        [HttpGet("invalid-records")]
        public async Task<IActionResult> GetInvalidRecords()
        {
            var list = await _db.MarketRecords
                .Where(r => !r.IsValid)
                .OrderByDescending(r => r.ReceivedAt)
                .Take(200)
                .ToListAsync();
            return Ok(list);
        }
    }
}
