using ApiAggregation.Services.News;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ApiAggregation.Controllers
{
    [ApiController]
    [Route("api/gnews")]
    public class GNewsController : ControllerBase
    {
        private readonly GNewsApiService _gNewsApiService;
        private readonly IMemoryCache _cache;
        public GNewsController(GNewsApiService gNewsService, IMemoryCache cache)
        {
            _gNewsApiService = gNewsService;
            _cache = cache;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return ValidationProblem("Topic is required for GNews searches.");
            }
            string cacheKey = $"gnews:search:{topic}";
            if (_cache.TryGetValue(cacheKey, out string? cachedResult))
            {
                return Ok(cachedResult);
            }
            var result = await _gNewsApiService.SearchNewsByTopicAsync(topic);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return Ok(result);
        }
    }
}
