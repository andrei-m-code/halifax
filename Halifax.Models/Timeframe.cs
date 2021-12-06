namespace Halifax.Models
{
    /// <summary>
    /// Open time interval
    /// </summary>
    public record Timeframe
    {
        public Timeframe()
        {
        }

        public Timeframe(DateTime? from, DateTime? to)
        {
        }

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}