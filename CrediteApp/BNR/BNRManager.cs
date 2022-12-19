using BNR.Models;

namespace BNR
{
    public class BNRManager
    {
        public static BNRResult DetailBNRResult()
        {
            BNRPayload payload = BNRPayloadProvider.ProvideBNRPayload();

            return new BNRResult
            {
                QuarterlyIndices = QuarterlyIndexCalculator.CalculateQuarterlyIndices(payload.DailyIndices),
                EuroExchangeRate = payload.EuroExchangeRate,
            };
        }
    }
}
