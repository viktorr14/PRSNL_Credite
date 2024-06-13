using System;

namespace LoanSubsystem.Models
{
    public class Instalment
    {
        public DateTime Date { get; set; }
        public decimal InterestPercentage { get; set; }
        public decimal RemainingSum { get; set; }
        public decimal InterestSum { get; set; }
        public decimal PrincipalSum { get; set; }
        public decimal TotalSum => InterestSum + PrincipalSum;
        public decimal AdditionalSum { get; set; }
        public decimal AdditionalComission { get; internal set; }
        public AdditionalRepayment[] AdditionalLoanRepayments { get; set; }
    }
}
