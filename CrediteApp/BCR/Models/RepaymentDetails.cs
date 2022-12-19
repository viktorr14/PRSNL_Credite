namespace Credit.Models
{
    public class RepaymentDetails
    {
        public RepaymentEntry[] RepaymentEntries { get; set; }

        public decimal TotalCost { get; set; }

        public decimal AdditonalRepaymentsSavings { get; set; }
    }
}
