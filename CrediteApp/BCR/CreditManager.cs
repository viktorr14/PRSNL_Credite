using Credit.Models;
using BNR.Models;
using System.Linq;

namespace Credit
{
    public class CreditManager
    {
        private CreditResource creditResource;

        public CreditDetails[] ListCredits => creditResource.ReadCredits();

        public CreditManager(CreditResource creditResource)
        {
            this.creditResource = creditResource;
        }

        public void SaveCredit(CreditDetails credit)
        {
            creditResource.WriteCredit(credit);
        }

        public static RepaymentDetails GetRepaymentDetails(CreditDetails creditDetails, QuarterlyIndex[] quarterlyIndices = null)
        {
            if (quarterlyIndices == null || creditDetails.InterestType == InterestType.Fixed)
            {
                quarterlyIndices = new QuarterlyIndex[] { new QuarterlyIndex() { Year = 2000, Quarter = 1 } };
            }

            return CreditCalculator.CalculateRepaymentDetails(creditDetails, quarterlyIndices);
        }

        public void SaveAdditionalLoanRepayment(CreditDetails credit, AdditionalLoanRepayment additionalLoanRepayment)
        {
            _ = credit.AdditionalLoanRepayments.RemoveAll(ar => ar.Date == additionalLoanRepayment.Date);

            if (additionalLoanRepayment.Sum > 0)
            {
                credit.AdditionalLoanRepayments.Add(additionalLoanRepayment);
            }

            credit.AdditionalLoanRepayments = credit.AdditionalLoanRepayments.OrderBy(ar => ar.Date).ToList();

            creditResource.WriteCredit(credit);
        }
    }
}
