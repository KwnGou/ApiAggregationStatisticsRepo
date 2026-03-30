using System.Net.Http.Headers;

namespace ApiAggregation.Services.Spotify
{
    public class SpotifyAuthHandler : DelegatingHandler
    {
        private readonly SpotifyAuthService _authService;

        public SpotifyAuthHandler(SpotifyAuthService authService)
        {
            _authService = authService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _authService.GetAccessTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
