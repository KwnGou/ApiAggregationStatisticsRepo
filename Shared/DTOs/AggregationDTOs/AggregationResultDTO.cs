namespace ApiAggregation.Models
{
    public class AggregationResultDTO
    {
        public object Spotify { get; set; } = new { };
        public object News { get; set; } = new { };
        public object? GitHub { get; set; }
    }
}
