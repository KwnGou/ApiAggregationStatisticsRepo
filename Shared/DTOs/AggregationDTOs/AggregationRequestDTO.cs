using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Shared.DTOs.AggregationDTOs
{
    /// <summary>
    /// Field to sort by. Allowed values are the enum names; serialized as strings.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SortBy
    {
        date
    }

    /// <summary>
    /// Sort order for results. Allowed values are the enum names; serialized as strings.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SortOrder
    {
        desc,
        asc
    }

    /// <summary>
    /// Parameters for the aggregation endpoint. Use query for Spotify search, topic for GNews, and GitHubOwner/GitHubRepo for GitHub details.
    /// DateFrom and DateTo must be provided in dd-MM-yyyy format (e.g., 31-12-2023) when using date-only input; model binding will attempt to parse date values.
    /// </summary>
    public class AggregationRequestDTO : IValidatableObject
    {
        /// <summary>
        /// Query used for Spotify searches. Example: track name or artist. This value is REQUIRED.
        /// Spotify uses this field to search tracks.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Maximum items to request per source. Default 5. Range 1-50.
        /// </summary>
        [Range(1, 50)]
        public int Limit { get; set; } = 5;

        /// <summary>
        /// GitHub owner used to fetch repository information. Must be provided together with <c>GitHubRepo</c>.
        /// </summary>
        public string? GitHubOwner { get; set; }

        /// <summary>
        /// GitHub repository name used to fetch repository information. Must be provided together with <c>GitHubOwner</c>.
        /// </summary>
        public string? GitHubRepo { get; set; }

        /// <summary>
        /// Start date filter (date-only). Prefer dd-MM-yyyy strings in query parameters; model binding will parse into DateTime.
        /// Note: GNews subscription only supports topic-based searches; dates will be ignored for news.
        /// </summary>
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [System.ComponentModel.Description("Format: mm-dd-yyyy")] // For Swagger UI placeholder
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// End date filter (date-only). Prefer dd-MM-yyyy strings in query parameters; model binding will parse into DateTime.
        /// Note: GNews subscription only supports topic-based searches; dates will be ignored for news.
        /// </summary>
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [System.ComponentModel.Description("Format: mm-dd-yyyy")] // For Swagger UI placeholder
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// Topic used for GNews article lookup (subscription supports topic-only search). Example: "technology", "sports".
        /// </summary>
        public string? Topic { get; set; }

        /// <summary>
        /// Field to sort by. Allowed values: date, source, relevance. Default: date.
        /// </summary>
        public SortBy SortBy { get; set; } = SortBy.date;

        /// <summary>
        /// Sort order for results. Allowed values: desc or asc. Default: desc.
        /// </summary>
        public SortOrder SortOrder { get; set; } = SortOrder.desc;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // GitHub owner/repo pairing
            if (!string.IsNullOrEmpty(GitHubOwner) ^ !string.IsNullOrEmpty(GitHubRepo))
            {
                yield return new ValidationResult(
                    "Both GitHubOwner and GitHubRepo must be provided together.",
                    new[] { nameof(GitHubOwner), nameof(GitHubRepo) });
            }

            // If both dates provided, ensure DateFrom <= DateTo
            if (DateFrom.HasValue && DateTo.HasValue)
            {
                // Compare date portion only
                var fromDate = DateFrom.Value.Date;
                var toDate = DateTo.Value.Date;
                if (fromDate > toDate)
                {
                    yield return new ValidationResult(
                        "DateFrom must be earlier than or equal to DateTo.",
                        new[] { nameof(DateFrom), nameof(DateTo) });
                }
            }
        }
    }
}
