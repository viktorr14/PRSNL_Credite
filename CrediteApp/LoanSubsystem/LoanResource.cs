using LoanSubsystem.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LoanSubsystem
{
    public class LoanResource
    {
        private readonly string filePath = @$"{Directory.GetCurrentDirectory()}\loans.json";

        public Loan[] ReadLoans()
        {
            if (!File.Exists(filePath))
                return Array.Empty<Loan>();

            return JsonConvert.DeserializeObject<Loan[]>(File.ReadAllText(filePath)) ?? Array.Empty<Loan>();
        }

        public void WriteLoan(Loan loan)
        {
            List<Loan> loans = !File.Exists(filePath)
                ? new List<Loan>()
                : JsonConvert.DeserializeObject<List<Loan>>(File.ReadAllText(filePath)) ?? new List<Loan>();

            Loan existingLoan = loans.FirstOrDefault(l => l.Id == loan.Id);
            if (existingLoan != null)
            {
                loans.RemoveAll(c => c.Id == existingLoan.Id);
            }

            loans.Add(loan);

            File.WriteAllText(filePath, JsonConvert.SerializeObject(loans.OrderBy(l => l.Id).ToArray(), Formatting.Indented));
        }
    }
}
