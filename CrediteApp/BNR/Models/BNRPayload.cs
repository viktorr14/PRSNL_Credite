namespace BNR.Models
{
    internal class BNRPayload
    {
        public DailyIndex[] DailyIndices { get; set; }

        public EuroExchangeRate EuroExchangeRate { get; set; }
    }
}
