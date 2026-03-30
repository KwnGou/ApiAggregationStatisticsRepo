using ApiAggregation.Services.Github;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ApiAggregation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitHubController : ControllerBase
    {
        private readonly GitHubService _service;
        private readonly IMemoryCache _cache;

        public GitHubController(GitHubService service, IMemoryCache cache)
        {
            _service = service;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string owner, string repo)
        {
            string cacheKey = $"github:{owner}:{repo}";
            if (_cache.TryGetValue(cacheKey, out object? cachedResult))
            {
                return Ok(cachedResult);
            }
            var result = await _service.GetRepositoryAsync(owner, repo);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return Ok(result);
        }
    }
}
