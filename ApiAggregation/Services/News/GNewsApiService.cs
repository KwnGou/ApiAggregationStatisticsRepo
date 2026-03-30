using System.Text;

namespace ApiAggregation.Services.News
{
    public class GNewsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GNewsApiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        // GNews subscription doesn't support real-time query/date filtering for this account.
        // Restrict call to topic-only search to match subscription capabilities.
        public async Task<string> SearchNewsByTopicAsync(string? topic, int limit = 5)
        {
            var apiKey = _config["GNews:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                // Return an empty articles structure as fallback so aggregation can continue
                return "{ \"articles\": [] }";
            }

            if (string.IsNullOrWhiteSpace(topic))
            {
                // If no topic provided, return empty to avoid abusive queries
                return "{ \"articles\": [] }";
            }

            var sb = new StringBuilder();
            sb.Append("https://gnews.io/api/v4/search?");
            sb.Append("q=");
            sb.Append(Uri.EscapeDataString(topic));

            sb.Append("&lang=en");
            sb.Append($"&max={Math.Clamp(limit, 1, 50)}");
            sb.Append($"&apikey={Uri.EscapeDataString(apiKey)}");

            var url = sb.ToString();

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
