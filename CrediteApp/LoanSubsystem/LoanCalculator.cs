using LoanSubsystem.Models;
using WebCrawlSubsystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LoanSubsystem
{
    internal static class LoanCalculator
    {
        internal static RepaymentDetails CalculateRepaymentDetails(Loan loan, QuarterlyIndex[] quarterlyIndices)
        {
            (List<Instalment> instalments, decimal totalCost) = Calculate(loan, quarterlyIndices);
            (_, decimal totalCostWithoutAdditionalRepayments) = Calculate(loan, quarterlyIndices, true);

            return new RepaymentDetails
            {
                Instalments = instalments.ToArray(),
                TotalCost = totalCost,
                AdditonalRepaymentsSavings = totalCostWithoutAdditionalRepayments - totalCost
            };
        }

        private static (List<Instalment>, decimal) Calculate(Loan loan, QuarterlyIndex[] quarterlyIndices, bool ignoreAdditionalRepayments = false)
        {
            DateTime currentInstalmentDate = new DateTime(loan.StartDate.Year, loan.StartDate.Month, loan.MonthlyDueDay);
            bool oddFirstMonth = false;
            int oddFirstMonthDays = 0;
            if (loan.StartDate.Day <= loan.MonthlyDueDay)
            {
                oddFirstMonth = true;
                oddFirstMonthDays = (currentInstalmentDate - loan.StartDate).Days + 1;
            }
            if (loan.StartDate.Day > loan.MonthlyDueDay)
            {
                oddFirstMonth = true;
                currentInstalmentDate = currentInstalmentDate.AddMonths(1);
                oddFirstMonthDays = (currentInstalmentDate - loan.StartDate).Days + 1;
            }

            decimal remainingSum = loan.Sum;
            int remainingMonths = loan.Duration;
            decimal totalCost = 0;

            DateTime lastPaymentDate = loan.StartDate;
            
            List<Instalment> instalments = new List<Instalment>(loan.Duration);

            while (remainingMonths > 0)
            {
                decimal irccPercentage = GetIrccPercentageInEffect(quarterlyIndices, currentInstalmentDate);
                decimal interestPercentage = irccPercentage + loan.InterestRate;
                decimal interestRate = interestPercentage / 100;
                decimal interestSum = oddFirstMonth
                    ? (interestRate / 12 * remainingSum * oddFirstMonthDays / (currentInstalmentDate - currentInstalmentDate.AddMonths(-1)).Days) 
                    : (interestRate / 12 * remainingSum);
                decimal mainSum = interestSum / (decimal)(Math.Pow(1 + ((double)interestRate / 12), remainingMonths) - 1);

                if (oddFirstMonth)
                {
                    oddFirstMonth = false;
                }    

                AdditionalRepayment[] additionalRepayments = loan.AdditionalRepayments.Where(ar => ar.Date > currentInstalmentDate.AddMonths(-1) && ar.Date <= currentInstalmentDate).ToArray();

                decimal additionalRepaidTotal = 0;
                decimal additionalRepaidComissionTotal = 0;
                DateTime previousInstalmentDate = currentInstalmentDate.AddMonths(-1);
                if (!ignoreAdditionalRepayments && additionalRepayments.Length != 0)
                {
                    decimal remainingInterestSum = interestSum;
                    interestSum = 0;

                    foreach (AdditionalRepayment additionalRepayment in additionalRepayments)
                    {
                        decimal additionalRepaymentSum = additionalRepayment.Sum;
                        int oldInterestDays = (additionalRepayment.Date - previousInstalmentDate).Days;
                        int newInterestDays = 30 - oldInterestDays;
                        decimal oldInterestSum = decimal.Round(interestRate / 12 * remainingSum * oldInterestDays / 30, 2);

                        additionalRepaidComissionTotal += loan.AdditionalRepaymentCommission * additionalRepaymentSum / 100;
                        additionalRepaymentSum = decimal.Round(additionalRepaymentSum, 2);
                        additionalRepaidTotal += additionalRepaymentSum;
                        remainingSum -= additionalRepaymentSum;

                        if (additionalRepayment.Type == AdditionalRepaymentType.PeriodReduction)
                        {
                            int monthsToReduce = (int)(additionalRepaymentSum / mainSum);

                            remainingMonths -= monthsToReduce == 0 ? 1 : monthsToReduce;
                        }

                        mainSum = interestRate / 12 * remainingSum / (decimal)(Math.Pow(1 + ((double)interestRate / 12), remainingMonths) - 1);
                        remainingInterestSum = decimal.Round(interestRate / 12 * remainingSum * newInterestDays / 30, 2);
                        interestSum += oldInterestSum;
                    }

                    interestSum += remainingInterestSum;
                }

                remainingSum -= decimal.Round(mainSum, 2);
                remainingMonths--;
                totalCost += decimal.Round(mainSum, 2) + decimal.Round(interestSum, 2) + decimal.Round(additionalRepaidTotal, 2);

                instalments.Add(new Instalment
                {
                    Date = currentInstalmentDate,
                    InterestPercentage = interestPercentage,
                    RemainingSum = decimal.Round(remainingSum, 2),
                    PrincipalSum = mainSum,
                    InterestSum = interestSum,
                    AdditionalSum = additionalRepaidTotal,
                    AdditionalComission = additionalRepaidComissionTotal,
                    AdditionalLoanRepayments = additionalRepayments
                });

                currentInstalmentDate = currentInstalmentDate.AddMonths(1);
            }

            return (instalments, totalCost);
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
