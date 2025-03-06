using WebCrawlSubsystem.Models;
using System;

namespace WebCrawlSubsystem
{
    internal static class QuarterlyIndexBuilder
    {
        internal static QuarterlyIndex BuildQuarterlyIndex(string rawQuarter, string rawPercentage)
        {
            int quarterNumber = int.Parse(rawQuarter[5..6]);
            DateTime quarterStartDate = new DateTime(int.Parse(rawQuarter[0..4]), quarterNumber * 3 - 2, 1);

            return new QuarterlyIndex
            {
                Year = quarterStartDate.Year,
                Quarter = quarterNumber,
                IndexPercentage = decimal.Parse(rawPercentage),
                IsCurrentlyInUse = QuarterlyIndexIsCurrentlyInUse(quarterStartDate),
            };
        }

        private static bool QuarterlyIndexIsCurrentlyInUse(DateTime quarterStartDate)
        {
            DateTime referenceDate = DateTime.Now.Date.AddMonths(-6);

            return referenceDate.Year == quarterStartDate.Year && referenceDate.Month >= quarterStartDate.Month && referenceDate.Month <= quarterStartDate.Month + 2; 
        }
    }
}