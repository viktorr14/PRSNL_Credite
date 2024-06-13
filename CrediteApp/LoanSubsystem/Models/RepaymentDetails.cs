namespace LoanSubsystem.Models
{
    public class RepaymentDetails
    {
        public Instalment[] Instalments { get; set; }

        public decimal TotalCost { get; set; }

        public decimal AdditonalRepaymentsSavings { get; set; }
    }
}
