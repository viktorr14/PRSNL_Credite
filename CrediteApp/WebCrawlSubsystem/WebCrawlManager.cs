using WebCrawlSubsystem.Models;

namespace WebCrawlSubsystem
{
    public class WebCrawlManager
    {
        public static WebCrawlResult DetailWebCrawlResult()
        {
            WebCrawlPayload payload = WebCrawlPayloadProvider.Provide();

            return new WebCrawlResult
            {
                QuarterlyIndices = QuarterlyIndexCalculator.CalculateQuarterlyIndices(payload.DailyIndices),
                EuroExchangeRate = payload.EuroExchangeRate,
            };
        }
    }
}
