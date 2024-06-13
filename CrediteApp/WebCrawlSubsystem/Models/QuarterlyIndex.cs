namespace WebCrawlSubsystem.Models
{
    public class QuarterlyIndex
    {
        public int Year { get; set; }

        public int Quarter { get; set; }

        public decimal IndexPercentage { get; set; }

        public bool IsCurrentlyInUse { get; set; }
    }
}
