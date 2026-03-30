using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.DTOs.Spotify
{
    /// <summary>
    /// Parameters for searching Spotify tracks.
    /// - <c>Query</c>: the search text, typically a track name or artist.
    /// - <c>Limit</c>: maximum number of tracks to return (1-50).
    /// </summary>
    public class SpotifySearchRequestDTO
    {
        /// <summary>
        /// Search term sent to Spotify's search endpoint. Example: "Never Gonna Give You Up" or artist name.
        /// This field is required.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of results to return from Spotify (default 5). Range 1-50.
        /// </summary>
        [Range(1, 50)]
        public int Limit { get; set; } = 5;
    }
}
