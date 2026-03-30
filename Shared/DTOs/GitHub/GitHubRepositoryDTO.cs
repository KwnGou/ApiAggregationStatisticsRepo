using System.Text.Json.Serialization;

namespace Shared.DTOs.GitHub
{
    public class GitHubRepositoryDTO
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
    }
}
