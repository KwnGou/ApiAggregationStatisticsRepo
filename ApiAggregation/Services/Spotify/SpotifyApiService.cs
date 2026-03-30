using System.Net.Http.Headers;
using System.Web;

namespace ApiAggregation.Services.Spotify
{
    public class SpotifyApiService
    {
        private readonly HttpClient _httpClient;

        public SpotifyApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SearchTracksAsync(string query, int limit = 5)
        {
            var escaped = Uri.EscapeDataString(query);
            var sanitizedLimit = Math.Clamp(limit, 1, 50);
            var url = $"v1/search?q={escaped}&type=track&limit={sanitizedLimit}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
