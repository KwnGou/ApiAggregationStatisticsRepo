using System.Text.Json.Serialization;

namespace Shared.DTOs.News
{
    public class GNewsResponseDTO
    {
        [JsonPropertyName("articles")]
        public List<GNewsArticleDTO> Articles { get; set; } = new();
    }

    public class GNewsArticleDTO
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("publishedAt")]
        public string? PublishedAt { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
