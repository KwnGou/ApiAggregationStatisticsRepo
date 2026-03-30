using ApiAggregation.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiAggregation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {

        private readonly AppDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> Get( DateOnly? fromDate, DateOnly? toDate)
        {
            if (_context.Statistics == null)
            {
                return NotFound();
            }
            List<Statistic> result;
            // Validate date range
            if (fromDate != null && toDate != null && fromDate > toDate )
            {
                return BadRequest("fromDate cannot be greater than toDate");
            }

            if (fromDate != null && toDate == null)
            {
                result = await _context.Statistics
                    .Where(s => s.RequestDate >= fromDate)
                    .ToListAsync();
            }
            else if (fromDate == null && toDate != null)
            {
                result = await _context.Statistics
                    .Where(s => s.RequestDate <= toDate)
                    .ToListAsync();
            }
            else if (fromDate != null && toDate != null)
            {
                result = await _context.Statistics
                    .Where(s => s.RequestDate >= fromDate && s.RequestDate <= toDate)
                    .ToListAsync();
            }
            else
            {
                result = await _context.Statistics
                    .ToListAsync();
            }

            return Ok(result);
        }
    }
}
