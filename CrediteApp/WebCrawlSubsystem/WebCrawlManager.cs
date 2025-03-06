using WebCrawlSubsystem.Models;

namespace WebCrawlSubsystem
{
    public class WebCrawlManager
    {
        public static WebCrawlResult GetWebCrawlPayLoad()
        {
            return WebCrawler.Crawl();
        }
    }
}