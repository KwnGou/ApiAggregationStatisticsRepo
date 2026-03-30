using ApiAggregation.Services.Spotify;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Shared.DTOs.Spotify;

namespace ApiAggregation.Controllers
{
    [ApiController]
    [Route("api/spotify")]
    public class SpotifyController : ControllerBase
    {
        private readonly SpotifyApiService _service;
        private readonly IMemoryCache _cache;

        public SpotifyController(SpotifyApiService service, IMemoryCache cache)
        {
            _service = service;
            _cache = cache;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] SpotifySearchRequestDTO req)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            string cacheKey = $"spotify:search:{req.Query}:{req.Limit}";
            if (_cache.TryGetValue(cacheKey, out string? cachedResult))
            {
                return Ok(cachedResult);
            }
            var result = await _service.SearchTracksAsync(req.Query, req.Limit);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return Ok(result);
        }
    }
}
