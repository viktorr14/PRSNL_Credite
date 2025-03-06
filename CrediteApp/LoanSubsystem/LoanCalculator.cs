using LoanSubsystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using WebCrawlSubsystem.Models;

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
            bool partialFirstMonth = false;
            int partialFirstMonthDays = 0;
            if (loan.StartDate.Day == loan.MonthlyDueDay + 1)
            {
                currentInstalmentDate = currentInstalmentDate.AddMonths(1);
            }
            if (loan.StartDate.Day <= loan.MonthlyDueDay)
            {
                partialFirstMonth = true;
                partialFirstMonthDays = (currentInstalmentDate - loan.StartDate).Days + 1;
            }
            if (loan.StartDate.Day > loan.MonthlyDueDay + 1)
            {
                partialFirstMonth = true;
                currentInstalmentDate = currentInstalmentDate.AddMonths(1);
                partialFirstMonthDays = (currentInstalmentDate - loan.StartDate).Days + 1;
            }

            decimal remainingSum = loan.Sum;
            int remainingMonths = loan.Duration;
            decimal totalCost = decimal.Zero;

            DateTime lastPaymentDate = loan.StartDate;

            List<Instalment> instalments = new List<Instalment>(loan.Duration);

            while (remainingMonths > 0)
            {
                decimal irccPercentage = GetIrccPercentageInEffect(quarterlyIndices, currentInstalmentDate);
                decimal interestPercentage = irccPercentage + loan.InterestRate;
                decimal interestRate = interestPercentage / 100;
                decimal interestSum = partialFirstMonth
                    ? (interestRate / 12 * remainingSum * partialFirstMonthDays / (currentInstalmentDate - currentInstalmentDate.AddMonths(-1)).Days)
                    : (interestRate / 12 * remainingSum);
                decimal principalSum = interestSum / (decimal)(Math.Pow(1 + ((double)interestRate / 12), remainingMonths) - 1);

                if (partialFirstMonth)
                {
                    partialFirstMonth = false;
                }

                AdditionalRepayment[] additionalRepayments = loan.AdditionalRepayments.Where(ar => ar.Date > currentInstalmentDate.AddMonths(-1) && ar.Date <= currentInstalmentDate).ToArray();

                decimal additionalRepaidTotal = decimal.Zero;
                decimal additionalRepaidComissionTotal = decimal.Zero;
                if (!ignoreAdditionalRepayments && additionalRepayments.Length != 0)
                {
                    DateTime previousInstalmentDate = currentInstalmentDate.AddMonths(-1);
                    int daysInMonth = (currentInstalmentDate - previousInstalmentDate).Days;

                    decimal intermediateInterestSum = decimal.Zero;
                    decimal recalculatedInterestSum = decimal.Zero;
                    decimal intermediatePrincipalSum = decimal.Zero;
                    foreach (AdditionalRepayment additionalRepayment in additionalRepayments)
                    {
                        int daysSinceLastPayment = (currentInstalmentDate - previousInstalmentDate).Days;
                        int accumulatedInterestDays = (additionalRepayment.Date - previousInstalmentDate).Days;
                        int leftoverInterestDays = daysSinceLastPayment - accumulatedInterestDays;
                        intermediateInterestSum += interestRate / 12 * remainingSum * accumulatedInterestDays / daysInMonth;

                        decimal additionalRepaymentSum = additionalRepayment.Sum + principalSum >= remainingSum ? remainingSum : additionalRepayment.Sum;
                        additionalRepaidComissionTotal += loan.AdditionalRepaymentCommission / 100 * additionalRepaymentSum;
                        additionalRepaidTotal += additionalRepaymentSum;
                        remainingSum -= additionalRepaymentSum;

                        if (remainingSum > decimal.Zero)
                        {
                            if (additionalRepayment.Type == AdditionalRepaymentType.PeriodReduction)
                            {
                                remainingMonths -= (int)(additionalRepaymentSum / principalSum);
                                decimal projectedPrincipalSum = interestRate / 12 * remainingSum / (decimal)(Math.Pow(1 + ((double)interestRate / 12), remainingMonths) - 1);
                                decimal nextMonthInterestSum = interestRate / 12 * (remainingSum - projectedPrincipalSum);
                                decimal nextMonthPrincipalSum = nextMonthInterestSum / (decimal)(Math.Pow(1 + ((double)interestRate / 12), remainingMonths - 1) - 1);
                                if (nextMonthInterestSum + nextMonthPrincipalSum > interestSum + principalSum)
                                {
                                    remainingMonths++;
                                }
                            }

                            recalculatedInterestSum = interestRate / 12 * remainingSum * leftoverInterestDays / daysInMonth;
                            intermediatePrincipalSum = interestRate / 12 * remainingSum / (decimal)(Math.Pow(1 + ((double)interestRate / 12), remainingMonths) - 1);
                            previousInstalmentDate = additionalRepayment.Date;
                        }
                        else
                        {
                            remainingMonths = 0;
                            principalSum = decimal.Zero;
                            recalculatedInterestSum = decimal.Zero;
                            break;
                        }
                    }
                    interestSum = intermediateInterestSum + recalculatedInterestSum;
                    principalSum = intermediatePrincipalSum;
                }

                remainingSum -= principalSum;
                totalCost += principalSum + interestSum + additionalRepaidTotal + additionalRepaidComissionTotal;

                instalments.Add(new Instalment
                {
                    Date = currentInstalmentDate,
                    InterestPercentage = interestPercentage,
                    RemainingSum = remainingSum,
                    PrincipalSum = principalSum,
                    InterestSum = interestSum,
                    AdditionalSum = additionalRepaidTotal,
                    AdditionalComission = additionalRepaidComissionTotal,
                    AdditionalLoanRepayments = additionalRepayments
                });

                currentInstalmentDate = currentInstalmentDate.AddMonths(1);
                remainingMonths--;
            }

            return (instalments, totalCost);
        }

        private static decimal GetIrccPercentageInEffect(QuarterlyIndex[] quarterlyIndices, DateTime currentEntryDate)
        {
            return quarterlyIndices.FirstOrDefault(quarterlyIndex => IsQuarterlyIndexEffective(quarterlyIndex, currentEntryDate))?.IndexPercentage ?? decimal.Zero;
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