using System;

namespace Credit.Models
{
    public class RepaymentEntry
    {
        public DateTime Date { get; set; }

        public decimal InterestPercentage { get; set; }

        public decimal RemainingSum { get; set; }

        public decimal RepaidInterestSum { get; set; }

        public decimal RepaidPrincipalSum { get; set; }

        public decimal RepaidTotalSum => RepaidInterestSum + RepaidPrincipalSum;

        public decimal RepaidAdditionalSum { get; set; }

        public decimal AdditionalRepaymentsSavings { get; set; }

        public AdditionalLoanRepayment[] AdditionalLoanRepayments { get; set; }
    }
}
