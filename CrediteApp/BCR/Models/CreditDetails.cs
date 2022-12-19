using System;
using System.Collections.Generic;

namespace Credit.Models
{
    public class CreditDetails
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal Sum { get; set; }

        public DateTime StartDate { get; set; }

        public int Duration { get; set; }

        public InterestType InterestType { get; set; }

        public decimal InterestRate { get; set; }

        public int MonthlyDueDay { get; set; }

        public decimal AdditionalLoanRepaymentCommission { get; set; }

        public List<AdditionalLoanRepayment> AdditionalLoanRepayments { get; set; }
    }

    public enum InterestType
    {
        Fixed = 0,
        Variable = 1
    }
}
