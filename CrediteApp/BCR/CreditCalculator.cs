using Credit.Models;
using BNR.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Credit
{
    internal static class CreditCalculator
    {
        internal static RepaymentDetails CalculateRepaymentDetails(CreditDetails creditDetails, QuarterlyIndex[] quarterlyIndices)
        {
            (List<RepaymentEntry> repaymentEntries, decimal totalCost) = Calculate(creditDetails, quarterlyIndices);
            (_, decimal totalCostWithoutAdditionalRepayments) = Calculate(creditDetails, quarterlyIndices, true);

            return new RepaymentDetails
            {
                RepaymentEntries = repaymentEntries.ToArray(),
                TotalCost = totalCost,
                AdditonalRepaymentsSavings = totalCostWithoutAdditionalRepayments - totalCost
            };
        }

        private static (List<RepaymentEntry>, decimal) Calculate(CreditDetails creditDetails, QuarterlyIndex[] quarterlyIndices, bool ignoreAdditionalRepayments = false)
        {
            int repaymentStartMonth = creditDetails.StartDate.Month;
            bool oddFirstMonth = false;
            int oddFirstMonthDays = 0;
            if (creditDetails.StartDate.Day < creditDetails.MonthlyDueDay)
            {
                oddFirstMonth = true;
                oddFirstMonthDays = (new DateTime(creditDetails.StartDate.Year, repaymentStartMonth, creditDetails.MonthlyDueDay) - creditDetails.StartDate).Days;
            }
            if (creditDetails.StartDate.Day > creditDetails.MonthlyDueDay)
            {
                oddFirstMonth = true;
                repaymentStartMonth++;
                oddFirstMonthDays = (new DateTime(creditDetails.StartDate.Year, repaymentStartMonth, creditDetails.MonthlyDueDay) - creditDetails.StartDate).Days;
            }

            DateTime currentEntryDate = new DateTime(creditDetails.StartDate.Year, repaymentStartMonth, creditDetails.MonthlyDueDay);

            decimal remainingSum = creditDetails.Sum;
            int remainingMonths = creditDetails.Duration;
            decimal totalCost = 0;

            DateTime lastPaymentDate = creditDetails.StartDate;
            
            List<RepaymentEntry> repaymentEntries = new List<RepaymentEntry>(creditDetails.Duration);

            while (remainingMonths > 0)
            {
                decimal irccPercentage = GetIrccPercentageInEffect(quarterlyIndices, currentEntryDate);
                decimal interestPercentage = irccPercentage + creditDetails.InterestRate;
                decimal interestRate = interestPercentage / 100;
                decimal interestSum = oddFirstMonth
                    ? (interestRate / 360 * remainingSum * oddFirstMonthDays) 
                    : (interestRate / 12 * remainingSum);
                decimal mainSum = interestSum / (decimal)(Math.Pow(1 + ((double)interestRate / 12), remainingMonths) - 1);

                if (oddFirstMonth)
                {
                    oddFirstMonth = false;
                }    

                AdditionalLoanRepayment[] additionalLoanRepayments = creditDetails.AdditionalLoanRepayments.Where(ar => ar.Date > currentEntryDate.AddMonths(-1) && ar.Date <= currentEntryDate).ToArray();

                decimal additionalRepaidTotal = 0;
                DateTime additionalRepaymentStartDate = currentEntryDate.AddMonths(-1);
                if (!ignoreAdditionalRepayments && additionalLoanRepayments.Length != 0)
                {
                    decimal remainingInterestSum = interestSum;
                    interestSum = 0;

                    foreach (AdditionalLoanRepayment additionalRepayment in additionalLoanRepayments)
                    {
                        decimal additionalRepaymentSum = additionalRepayment.Sum;

                        int daysInMonth = (currentEntryDate - currentEntryDate.AddMonths(-1)).Days;
                        int oldInterestDays = (additionalRepayment.Date - additionalRepaymentStartDate).Days;
                        int newInterestDays = (currentEntryDate - additionalRepayment.Date).Days;

                        additionalRepaymentStartDate = additionalRepayment.Date.AddDays(1);

                        decimal oldInterestSum = decimal.Round(interestRate / 360 * remainingSum * oldInterestDays, 2);
                        additionalRepaymentSum -= oldInterestSum;

                        decimal additionalRepaymentCommission = creditDetails.AdditionalLoanRepaymentCommission * additionalRepaymentSum / (100 + creditDetails.AdditionalLoanRepaymentCommission);
                        additionalRepaymentSum -= additionalRepaymentCommission;

                        additionalRepaymentSum = decimal.Round(additionalRepaymentSum, 2);

                        additionalRepaidTotal += additionalRepaymentSum;

                        remainingSum -= additionalRepaymentSum;

                        if (additionalRepayment.Type == AdditionalLoanRepaymentType.PeriodReduction)
                        {
                            int monthsToReduce = (int)(additionalRepaymentSum / mainSum);

                            remainingMonths -= monthsToReduce == 0 ? 1 : monthsToReduce;
                        }

                        mainSum = interestRate / 12 * remainingSum / (decimal)(Math.Pow(1 + ((double)interestRate / 12), remainingMonths) - 1);

                        remainingInterestSum = decimal.Round(interestRate / 360 * remainingSum * newInterestDays, 2);

                        interestSum += oldInterestSum;
                    }

                    interestSum += remainingInterestSum;
                }

                remainingSum -= decimal.Round(mainSum, 2);

                remainingMonths--;

                totalCost += decimal.Round(mainSum, 2) + decimal.Round(interestSum, 2) + decimal.Round(additionalRepaidTotal, 2);

                repaymentEntries.Add(new RepaymentEntry
                {
                    Date = currentEntryDate,
                    InterestPercentage = interestPercentage,
                    RemainingSum = decimal.Round(remainingSum, 2),
                    RepaidPrincipalSum = mainSum,
                    RepaidInterestSum = interestSum,
                    RepaidAdditionalSum = additionalRepaidTotal,
                    AdditionalLoanRepayments = additionalLoanRepayments
                });

                currentEntryDate = currentEntryDate.AddMonths(1);
            }

            return (repaymentEntries, totalCost);
        }

        private static decimal GetIrccPercentageInEffect(QuarterlyIndex[] quarterlyIndices, DateTime currentEntryDate)
        {
            return quarterlyIndices.FirstOrDefault(quarterlyIndex => IsQuarterlyIndexEffective(quarterlyIndex, currentEntryDate))?.IndexPercentage ?? quarterlyIndices.First().IndexPercentage;
        }

        private static bool IsQuarterlyIndexEffective(QuarterlyIndex quarterlyIndex, DateTime currentEntryDate)
        {
            DateTime quarterStartDate = GetQuarterlyIndexApplicability(quarterlyIndex);
            DateTime quarterEndDate = GetQuarterlyIndexApplicability(quarterlyIndex, true);

            if (currentEntryDate.AddDays(1).Month == currentEntryDate.Month)
            {
                currentEntryDate = new DateTime(currentEntryDate.Year, currentEntryDate.Month, 1).AddDays(-1);
            }

            return currentEntryDate >= quarterStartDate && currentEntryDate <= quarterEndDate;
        }

        private static DateTime GetQuarterlyIndexApplicability(QuarterlyIndex quarterlyIndex, bool endDate = false)
        {
            DateTime quarterlyIndexApplicabilityStartDate = new DateTime(quarterlyIndex.Year, (quarterlyIndex.Quarter - 1) * 3 + 1, 1).AddMonths(6);

            if (endDate)
                return quarterlyIndexApplicabilityStartDate.AddMonths(4).AddDays(-1);

            return quarterlyIndexApplicabilityStartDate;
        }
    }
}
