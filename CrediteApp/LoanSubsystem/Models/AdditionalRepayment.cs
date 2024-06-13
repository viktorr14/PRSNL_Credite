using System;

namespace LoanSubsystem.Models
{
    public class AdditionalRepayment
    {
        public string Name { get; set; }

        public DateTime Date { get; set; }

        public decimal Sum { get; set; }

        public AdditionalRepaymentType Type { get; set; }
    }

    public enum AdditionalRepaymentType
    {
        PeriodReduction = 0,
        PaymentReduction = 1
    }
}
