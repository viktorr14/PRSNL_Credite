using System;

namespace WebCrawlSubsystem.Models
{
    public class EuroExchangeRate
    {
        public DateTime Date { get; set; }

        public decimal Value { get; set; }

        public decimal PreviousDateChange { get; set; }
    }
}
