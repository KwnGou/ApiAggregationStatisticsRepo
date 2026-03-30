using System.Text.Json;

namespace Shared.DTOs.AggregationDTOs
{
    public class AggregationItemDTO
    {
        public string Source { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public DateTimeOffset? Date { get; set; }
        public JsonElement? Raw { get; set; }
    }
}
