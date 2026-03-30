using ApiAggregation.Models;
using ApiAggregation.Services.Github;
using ApiAggregation.Services.News;
using ApiAggregation.Services.Spotify;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Memory;
using Shared;
using Shared.DTOs.AggregationDTOs;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;


namespace ApiAggregation.Controllers
{
    [ApiController]
    [Route("api/aggregate")]
    public class AggregationController : ControllerBase
    {
        private readonly SpotifyApiService _spotify;
        private readonly GNewsApiService _news;
        private readonly GitHubService _github;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private Options options;
        private AppDbContext appDbContext;

        public AggregationController(SpotifyApiService spotify, GNewsApiService news, GitHubService github, IMemoryCache cache, IConfiguration config, AppDbContext context)
        {
            _spotify = spotify;
            _news = news;
            _github = github;
            _cache = cache;
            _configuration = config;
            _context = context;
            options = _configuration.GetSection("Options").Get<Options>();
            if (options == null)
                throw new InvalidOperationException("Options configuration section is missing or invalid.");
        }

        private async Task<ApiResult> ExecuteWithTimeout<T>(Func<Task<T>> func, string apiName)
        {
            var stopWatch = new Stopwatch();
            ApiResult result = new ApiResult();
            var logEntry = new Statistic
            {
                Api = apiName,
                RequestDate = DateOnly.FromDateTime(DateTime.Today)
            };
            try
            {
                stopWatch.Start();

                result.Response = await func().WaitAsync(TimeSpan.FromSeconds(options.Timeout));

                stopWatch.Stop();

                if (stopWatch.ElapsedMilliseconds < options.ResponseFast)
                {
                    logEntry.ResponseFast += 1;
                }
                else if (stopWatch.ElapsedMilliseconds >= options.ResponseFast && stopWatch.ElapsedMilliseconds <= options.ResponseAvg)
                {
                    logEntry.RespsonseAverage += 1;
                }
                else if (stopWatch.ElapsedMilliseconds > options.ResponseAvg)
                {
                    logEntry.ResponseSlow += 1;
                }

            }
            catch (TimeoutException)
            {
                stopWatch.Stop();
                logEntry.TimedOut += 1;
            }
            catch (Exception ex)
            {
                stopWatch.Stop();
                logEntry.Failed += 1;
            }
            result.Statistic = logEntry;

            return result;
        }
        /// <summary>
        /// Get aggregated results from multiple sources.
        /// </summary>
        /// <remarks>
        /// Request fields (passed as query parameters):
        /// 
        /// - <b>Query</b> (required): text used to search Spotify tracks (track name, artist, etc.). Example: "never gonna give you up".
        /// - <b>Limit</b>: maximum items to request per source (1-50). Default 5.
        /// - <b>Topic</b>: topic/category sent to GNews (subscription supports topic-only searches). Example: "technology". If absent, news results are omitted.
        /// - <b>DateFrom</b> / <b>DateTo</b>: date range in <c>dd-MM-yyyy</c> format (e.g., 31-12-2023).
        /// - <b>GitHubOwner</b> and <b>GitHubRepo</b>: when both provided, fetch GitHub repository details (owner + repo name).
        /// - <b>SortBy</b>: which field to sort by. Allowed enum values: <c>date</c>, <c>source</c>, <c>relevance</c>. Currently only <c>date</c> is applied.
        /// - <b>SortOrder</b>: ordering direction. Allowed enum values: <c>desc</c>, <c>asc</c>. Default is <c>desc</c> (newest first).
        /// 
        /// The endpoint runs Spotify and (optionally) GNews and GitHub requests in parallel and returns a unified list of items.
        /// Each item includes <c>source</c>, <c>title</c>, optional <c>url</c>, optional <c>date</c>, and the raw provider JSON in <c>raw</c>.
        /// </remarks>
        /// <param name="req">Aggregation request parameters (from query string).</param>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] AggregationRequestDTO req)
        {
            const string SPOTIFY = "SpotifyAPI";
            const string NEWS = "NewsAPI";
            const string GITHUB = "GithubAPI";
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Get log entries for today
            var logEntrySpotify = await _context.Statistics.SingleOrDefaultAsync(s => s.Api == SPOTIFY && s.RequestDate == today);
            var logEntryNews = await _context.Statistics.SingleOrDefaultAsync(s => s.Api == NEWS && s.RequestDate == today);
            var logEntryGitHub = await _context.Statistics.SingleOrDefaultAsync(s => s.Api == GITHUB && s.RequestDate == today);

            string cacheKey = $"aggregate:{req.Query}:{req.Limit}:{req.Topic}:{req.GitHubOwner}:{req.GitHubRepo}:{req.DateFrom}:{req.DateTo}:{req.SortBy}:{req.SortOrder}";
            if (_cache.TryGetValue(cacheKey, out object? cachedResult))
            {
                // Batch statistics updates to minimize DB context usage
                var statsToUpdate = new List<Statistic>();

                if (logEntrySpotify == null) logEntrySpotify = new Statistic { Api = SPOTIFY, RequestDate = today };
                {
                    logEntrySpotify.Cached += 1;
                    statsToUpdate.Add(logEntrySpotify);
                }
                if (!string.IsNullOrWhiteSpace(req.Topic))
                {
                    if (logEntryNews == null) logEntryNews = new Statistic { Api = NEWS, RequestDate = today };
                    logEntryNews.Cached += 1;
                    statsToUpdate.Add(logEntryNews);
                }
                if (!string.IsNullOrEmpty(req.GitHubOwner) && !string.IsNullOrEmpty(req.GitHubRepo))
                {
                    if (logEntryGitHub == null) logEntryGitHub = new Statistic { Api = GITHUB, RequestDate = today };
                    logEntryGitHub.Cached += 1;
                    statsToUpdate.Add(logEntryGitHub);
                }

                foreach (var stat in statsToUpdate)
                {
                    if (stat.Id == 0)
                        _context.Statistics.Add(stat);
                    else
                        _context.Statistics.Update(stat);
                }

                _context.SaveChanges();

                return Ok(cachedResult);
            }

            var errors = new List<object>();

            Task<ApiResult> spotifyTask = null;
            Task<ApiResult> newsTask = null;
            Task<ApiResult> githubTask = null;

            spotifyTask = ExecuteWithTimeout(() => _spotify.SearchTracksAsync(req.Query, req.Limit), SPOTIFY);
            if (!string.IsNullOrWhiteSpace(req.Topic))
            {
                newsTask = ExecuteWithTimeout(() => _news.SearchNewsByTopicAsync(req.Topic, req.Limit), NEWS);
            }
            if (!string.IsNullOrEmpty(req.GitHubOwner) && !string.IsNullOrEmpty(req.GitHubRepo))
            {
                githubTask = ExecuteWithTimeout(() => _github.GetRepositoryAsync(req.GitHubOwner, req.GitHubRepo), GITHUB);
            }

            try
            {
                var tasks = new List<Task> { spotifyTask };
                if (newsTask is not null) tasks.Add(newsTask);
                if (githubTask is not null) tasks.Add(githubTask);
                await Task.WhenAll(tasks);
            }
            catch
            {
                // swallow: individual try/catch will handle specifics
            }

            var items = new List<AggregationItemDTO>();

            // Map Spotify results
            try
            {
                var spotifyJson = await spotifyTask;
                using var doc = JsonDocument.Parse(spotifyJson.Response.ToString());
                var root = doc.RootElement;
                // Spotify search: root["tracks"]["items"]
                if (root.TryGetProperty("tracks", out var tracks) && tracks.TryGetProperty("items", out var itemsElem) && itemsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in itemsElem.EnumerateArray())
                    {
                        var name = t.GetProperty("name").GetString() ?? string.Empty;
                        DateTimeOffset? date = null;
                        // Spotify track doesn't include a publish date at track level; try album.release_date
                        if (t.TryGetProperty("album", out var album) && album.TryGetProperty("release_date", out var rd))
                        {
                            if (DateTimeOffset.TryParse(rd.GetString(), out var parsed)) date = parsed;
                        }

                        string? url = null;
                        if (t.TryGetProperty("external_urls", out var ext) && ext.TryGetProperty("spotify", out var spUrl))
                        {
                            url = spUrl.GetString();
                        }

                        items.Add(new AggregationItemDTO
                        {
                            Source = "spotify",
                            Title = name,
                            Url = url,
                            Date = date,
                            Raw = t.Clone()
                        });
                    }
                }
                // Add statistic
                if (logEntrySpotify == null)
                {
                    logEntrySpotify = new Statistic
                    {
                        Api = SPOTIFY,
                        RequestDate = today,
                        ResponseFast = spotifyJson.Statistic.ResponseFast,
                        RespsonseAverage = spotifyJson.Statistic.RespsonseAverage,
                        ResponseSlow = spotifyJson.Statistic.ResponseSlow
                    };
                    _context.Statistics.Add(logEntrySpotify);
                }
                else
                {
                    logEntrySpotify.ResponseFast = spotifyJson.Statistic.ResponseFast;
                    logEntrySpotify.RespsonseAverage = spotifyJson.Statistic.RespsonseAverage;
                    logEntrySpotify.ResponseSlow = spotifyJson.Statistic.ResponseSlow;
                    _context.Statistics.Update(logEntrySpotify);
                }
            }
            catch (Exception ex)
            {
                errors.Add(new { source = "spotify", message = ex.Message });
                // fallback: add error item
                items.Add(new AggregationItemDTO
                {
                    Source = "spotify",
                    Title = "Spotify data unavailable",
                    Url = null,
                    Date = null,
                    Raw = null
                });
            }

            // Map News results
            if (newsTask is not null)
            {
                try
                {
                    var newsJson = await newsTask;
                    using var doc = JsonDocument.Parse(newsJson.Response.ToString());
                    var root = doc.RootElement;
                    if (root.TryGetProperty("articles", out var articles) && articles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var a in articles.EnumerateArray())
                        {
                            var title = a.GetProperty("title").GetString() ?? string.Empty;
                            DateTimeOffset? date = null;
                            if (a.TryGetProperty("publishedAt", out var pa) && DateTimeOffset.TryParse(pa.GetString(), out var parsed)) date = parsed;
                            string? url = a.TryGetProperty("url", out var u) ? u.GetString() : null;

                            items.Add(new AggregationItemDTO
                            {
                                Source = "news",
                                Title = title,
                                Url = url,
                                Date = date,
                                Raw = a.Clone()
                            });
                        }
                    }
                    // Add statistic
                    if (logEntryNews == null)
                    {
                        logEntryNews = new Statistic
                        {
                            Api = NEWS,
                            RequestDate = today,
                            ResponseFast = newsJson.Statistic.ResponseFast,
                            RespsonseAverage = newsJson.Statistic.RespsonseAverage,
                            ResponseSlow = newsJson.Statistic.ResponseSlow
                        };
                        _context.Statistics.Add(logEntryNews);
                    }
                    else
                    {
                        logEntryNews.ResponseFast = newsJson.Statistic.ResponseFast;
                        logEntryNews.RespsonseAverage = newsJson.Statistic.RespsonseAverage;
                        logEntryNews.ResponseSlow = newsJson.Statistic.ResponseSlow;
                        _context.Statistics.Update(logEntryNews);
                    }

                }
                catch (Exception ex)
                {
                    errors.Add(new { source = "news", message = ex.Message });
                    items.Add(new AggregationItemDTO
                    {
                        Source = "news",
                        Title = "News data unavailable",
                        Url = null,
                        Date = null,
                        Raw = null
                    });
                }
            }

            // Map GitHub if present (simple mapping)
            if (githubTask is not null)
            {
                try
                {
                    var gh = await githubTask;
                    if (gh.Response is JsonElement ge)
                    {
                        string title = ge.GetProperty("full_name").GetString() ?? string.Empty;
                        DateTimeOffset? date = null;
                        if (ge.TryGetProperty("created_at", out var ca) && DateTimeOffset.TryParse(ca.GetString(), out var parsed)) date = parsed;
                        string? url = ge.TryGetProperty("html_url", out var hu) ? hu.GetString() : null;

                        items.Add(new AggregationItemDTO
                        {
                            Source = "github",
                            Title = title,
                            Url = url,
                            Date = date,
                            Raw = ge.Clone()
                        });
                    }
                    else if (gh is object)
                    {
                        items.Add(new AggregationItemDTO
                        {
                            Source = "github",
                            Title = gh.ToString() ?? string.Empty,
                            Raw = default
                        });
                    }
                    // Add statistic
                    if (logEntryGitHub == null)
                    {
                        logEntryGitHub = new Statistic
                        {
                            Api = GITHUB,
                            RequestDate = today,
                            ResponseFast = gh.Statistic.ResponseFast,
                            RespsonseAverage = gh.Statistic.RespsonseAverage,
                            ResponseSlow = gh.Statistic.ResponseSlow
                        };
                        _context.Statistics.Add(logEntryGitHub);
                    }
                    else
                    {
                        logEntryGitHub.ResponseFast = gh.Statistic.ResponseFast;
                        logEntryGitHub.RespsonseAverage = gh.Statistic.RespsonseAverage;
                        logEntryGitHub.ResponseSlow = gh.Statistic.ResponseSlow;
                        _context.Statistics.Update(logEntryGitHub);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new { source = "github", message = ex.Message });
                    items.Add(new AggregationItemDTO
                    {
                        Source = "github",
                        Title = "GitHub data unavailable",
                        Url = null,
                        Date = null,
                        Raw = null
                    });
                }
            }

            // Filter by date range if provided
            if (req.DateFrom.HasValue || req.DateTo.HasValue)
            {
                var from = req.DateFrom?.Date ?? DateTime.MinValue;
                var to = req.DateTo?.Date ?? DateTime.MaxValue;
                items = items.Where(i =>
                    i.Date.HasValue &&
                    i.Date.Value.Date >= from &&
                    i.Date.Value.Date <= to
                ).ToList();
            }

            // Sort by date
            if (req.SortBy == SortBy.date)
            {
                if (req.SortOrder == SortOrder.desc)
                {
                    items = items.OrderByDescending(i => i.Date ?? DateTimeOffset.MinValue).ToList();
                }
                else
                {
                    items = items.OrderBy(i => i.Date ?? DateTimeOffset.MaxValue).ToList();
                }
            }
            // Save all statistics
            await _context.SaveChangesAsync();
            
            var result = new { meta = new { count = items.Count, sortBy = req.SortBy.ToString(), sortOrder = req.SortOrder.ToString(), errors }, items };
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return Ok(result);
        }
    }
}
