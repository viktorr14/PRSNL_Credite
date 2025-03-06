using System;

namespace LoanSubsystem.Models
{
    public class AdditionalRepayment
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Sum { get; set; }
        public AdditionalRepaymentType Type { get; set; }

        public string DisplayName => Id == 0 ? "Plată nouă..." : $"{Date:dd.MM.yyyy} ({Id}{IdSuffix})";
        private string IdSuffix => (Id % 10) switch
        {
            1 when Id / 10 % 10 != 1 => "st",
            2 when Id / 10 % 10 != 1 => "nd",
            3 when Id / 10 % 10 != 1 => "rd",
            _ => "th",
        };
    }

    public enum AdditionalRepaymentType
    {
        PeriodReduction = 0,
        PaymentReduction = 1
    }
}