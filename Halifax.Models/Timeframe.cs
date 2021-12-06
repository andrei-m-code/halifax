namespace Halifax.Models
{
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