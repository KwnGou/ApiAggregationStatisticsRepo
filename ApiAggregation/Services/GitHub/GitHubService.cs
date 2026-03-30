using System.Net.Http.Headers;

namespace ApiAggregation.Services.Github
{


    public class GitHubService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GitHubService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<object?> GetRepositoryAsync(string owner, string repo)
        {
            var token = _config["GitHub:Token"];

            using var request = new HttpRequestMessage(HttpMethod.Get, $"repos/{owner}/{repo}");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            request.Headers.UserAgent.ParseAdd("MyAggregatorApp");

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<object>();
        }
    }
}
