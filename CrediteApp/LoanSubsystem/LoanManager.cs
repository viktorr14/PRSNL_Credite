using LoanSubsystem.Models;
using WebCrawlSubsystem.Models;
using System.Linq;
using System;

namespace LoanSubsystem
{
    public class LoanManager
    {
        private readonly LoanResource loanResource;

        public Loan[] ListCredits => loanResource.ReadLoans();

        public LoanManager(LoanResource loanResource)
        {
            this.loanResource = loanResource;
        }

        public void SaveLoan(Loan loan)
        {
            loanResource.WriteLoan(loan);
        }

        public static RepaymentDetails GetRepaymentDetails(Loan loan, QuarterlyIndex[] quarterlyIndices = null)
        {
            if (quarterlyIndices == null || loan.InterestType == InterestType.Fixed)
            {
                quarterlyIndices = new QuarterlyIndex[0];
            }

            return LoanCalculator.CalculateRepaymentDetails(loan, quarterlyIndices);
        }

        public void DeleteAdditionalRepayment(Loan loan, int id)
        {
            loan.AdditionalRepayments.RemoveAll(ar => ar.Id == id);

            loanResource.WriteLoan(loan);
        }

        public void SaveAdditionalRepayment(Loan loan, AdditionalRepayment additionalRepayment)
        {
            loan.AdditionalRepayments.RemoveAll(ar => ar.Id == additionalRepayment.Id);

            if (additionalRepayment.Sum > 0)
            {
                loan.AdditionalRepayments.Add(additionalRepayment);
            }

            loan.AdditionalRepayments = loan.AdditionalRepayments.OrderBy(ar => ar.Date).ToList();

            loanResource.WriteLoan(loan);
        }
    }
}
