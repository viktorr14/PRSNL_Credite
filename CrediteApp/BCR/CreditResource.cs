using Credit.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Credit
{
    public class CreditResource
    {
        private readonly string filePath = @$"{Directory.GetCurrentDirectory()}\credits.json";

        public CreditDetails[] ReadCredits()
        {
            if (!File.Exists(filePath))
                return new CreditDetails[0];

            return JsonConvert.DeserializeObject<CreditDetails[]>(File.ReadAllText(filePath)) ?? new CreditDetails[0];
        }

        public void WriteCredit(CreditDetails credit)
        {
            List<CreditDetails> credits = !File.Exists(filePath)
                ? new List<CreditDetails>()
                : JsonConvert.DeserializeObject<List<CreditDetails>>(File.ReadAllText(filePath)) ?? new List<CreditDetails>();

            CreditDetails existingCredit = credits.FirstOrDefault(c => c.Id == credit.Id);
            if (existingCredit != null)
            {
                credits.RemoveAll(c => c.Id == existingCredit.Id);
            }

            credits.Add(credit);

            CreditDetails[] creditsToWrite = credits.OrderBy(credit => credit.Id).ToArray();

            string serializedCredits = JsonConvert.SerializeObject(creditsToWrite, Formatting.Indented);

            File.WriteAllText(filePath, serializedCredits);
        }
    }
}
