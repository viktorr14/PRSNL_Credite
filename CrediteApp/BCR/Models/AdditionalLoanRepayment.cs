using System;

namespace Credit.Models
{
    public class AdditionalLoanRepayment
    {
        public string Name { get; set; }

        public DateTime Date { get; set; }

        public decimal Sum { get; set; }

        public AdditionalLoanRepaymentType Type { get; set; }
    }

    public enum AdditionalLoanRepaymentType
    {
        PeriodReduction = 0,
        PaymentReduction = 1
    }
}
