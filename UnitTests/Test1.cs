using ApiAggregation;
using ApiAggregation.Controllers;
using ApiAggregation.Models;
using ApiAggregation.Services.Github;
using ApiAggregation.Services.News;
using ApiAggregation.Services.Spotify;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Shared.DTOs.AggregationDTOs;

namespace UnitTests
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public async Task TestAggregationController_ShouldSucced()
        {
            
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(config.GetConnectionString("DefaultConnection"))
                .Options);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var spotifyService = new SpotifyApiService(new HttpClient());   
            var newsService = new GNewsApiService(new HttpClient(), config);
            var githubService = new GitHubService(new HttpClient(), config);

            var controller = new AggregationController(spotifyService, newsService, githubService, cache, config, context);
            var query = new AggregationRequestDTO
            {
                Query = "metalica",
                Limit = 5,
                GitHubOwner = "KwnGou",
                GitHubRepo = "MenuListApp",
                Topic = "trump",
            };

            var result = await controller.Get(query);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IActionResult));
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }
    }
}
