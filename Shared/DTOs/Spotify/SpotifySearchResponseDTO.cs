using System.Text.Json.Serialization;

namespace Shared.DTOs.Spotify
{
    public class SpotifySearchResponseDTO
    {
        [JsonPropertyName("tracks")]
        public SpotifyTracksDTO Tracks { get; set; } = new();
    }

    public class SpotifyTracksDTO
    {
        [JsonPropertyName("items")]
        public List<SpotifyTrackDTO> Items { get; set; } = new();
    }

    public class SpotifyTrackDTO
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("album")]
        public SpotifyAlbumDTO Album { get; set; } = new();

        [JsonPropertyName("external_urls")]
        public SpotifyExternalUrlsDTO ExternalUrls { get; set; } = new();
    }

    public class SpotifyAlbumDTO
    {
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
    }

    public class SpotifyExternalUrlsDTO
    {
        [JsonPropertyName("spotify")]
        public string? Spotify { get; set; }
    }
}
