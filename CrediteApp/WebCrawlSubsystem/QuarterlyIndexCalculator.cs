using WebCrawlSubsystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebCrawlSubsystem
{
    internal static class QuarterlyIndexCalculator
    {
        internal static QuarterlyIndex[] CalculateQuarterlyIndices(DailyIndex[] dailyIndices)
        {
            List<QuarterlyIndex> quarterlyIndices = new List<QuarterlyIndex>();

            DailyIndex[] dailyIndicesByDateDescending = dailyIndices.OrderByDescending(dailyIndex => dailyIndex.Date).ToArray();

            DateTime previousDailyIndexDate = dailyIndicesByDateDescending[0].Date;
            int currentQuarterCount = 1;
            decimal currentQuarterSum = dailyIndicesByDateDescending[0].IndexPercentage;

            for (int index = 1; index < dailyIndicesByDateDescending.Length; index++)
            {
                DailyIndex dailyIndexToProcess = dailyIndicesByDateDescending[index];

                if (IsQuarterStartMonth(previousDailyIndexDate.Month) && IsQuarterEndMonth(dailyIndexToProcess.Date.Month))
                {
                    quarterlyIndices.Add(new QuarterlyIndex
                    {
                        Year = previousDailyIndexDate.Year,
                        Quarter = (previousDailyIndexDate.Month + 2) / 3,
                        IndexPercentage = decimal.Round(currentQuarterSum / currentQuarterCount, 2),
                        IsCurrentlyInUse = QuarterlyIndexIsCurrentlyInUse(previousDailyIndexDate),
                    });;

                    currentQuarterCount = 1;
                    currentQuarterSum = dailyIndexToProcess.IndexPercentage;
                }
                else
                {
                    currentQuarterCount++;
                    currentQuarterSum += dailyIndexToProcess.IndexPercentage;
                }

                previousDailyIndexDate = dailyIndexToProcess.Date;
            }

            quarterlyIndices.Add(new QuarterlyIndex
            {
                Year = previousDailyIndexDate.Year,
                Quarter = (previousDailyIndexDate.Month + 2) / 3,
                IndexPercentage = decimal.Round(currentQuarterSum / currentQuarterCount, 2),
                IsCurrentlyInUse = QuarterlyIndexIsCurrentlyInUse(previousDailyIndexDate),
            });

            return quarterlyIndices.ToArray();
        }

        private static bool QuarterlyIndexIsCurrentlyInUse(DateTime quarterStartDate)
        {
            DateTime referenceDate = DateTime.Now.AddMonths(-6);

            return referenceDate.Year == quarterStartDate.Year && referenceDate.Month >= quarterStartDate.Month && referenceDate.Month <= quarterStartDate.Month + 2; 
        }

        private static bool IsQuarterStartMonth(int month)
        {
            return (month - 1) % 3 == 0;
        }

        private static bool IsQuarterEndMonth(int month)
        {
            return month % 3 == 0;
        }
    }
}
