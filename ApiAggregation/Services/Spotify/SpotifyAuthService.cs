using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace ApiAggregation.Services.Spotify
{
    public class SpotifyAuthService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        // Cached token and expiry
        private string? _cachedToken;
        private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public SpotifyAuthService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // Return cached token if still valid
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            await _semaphore.WaitAsync();
            try
            {
                // Double-check after acquiring semaphore
                if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiry)
                {
                    return _cachedToken;
                }

                var clientId = _config["Spotify:ClientId"];
                var clientSecret = _config["Spotify:ClientSecret"];

                var authHeader = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")
                );

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" }
                });

                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var token = root.GetProperty("access_token").GetString();

                // Try to read expires_in, fallback to 1 hour
                int expiresIn = 3600;
                if (root.TryGetProperty("expires_in", out var expiresProp) && expiresProp.ValueKind == JsonValueKind.Number)
                {
                    expiresIn = expiresProp.GetInt32();
                }

                // Subtract a small buffer to avoid using an expired token
                var expiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);

                _cachedToken = token;
                _tokenExpiry = expiry;

                return _cachedToken!;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
