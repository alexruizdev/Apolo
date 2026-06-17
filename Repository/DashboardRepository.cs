using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public class DashboardRepository(ApoloContext context, ApoloArchiveContext archiveDb) : IDashboardRepository
    {
        private readonly ApoloContext _context = context;
        private readonly ApoloArchiveContext _archiveDb = archiveDb;

        public async Task<decimal> GetTotalUnpaidAmountAsync()
        {
            return await _context.Set<Lesson>()
                .Where(l => !l.IsPaid)
                .SumAsync(l => l.FinalPrice);
        }

        public async Task<decimal> GetMonthlyEarningsAsync(int year, int month)
        {
            return await _context.Set<Lesson>()
                .Where(l => l.Date.Year == year && l.Date.Month == month && l.IsPaid)
                .SumAsync(l => l.FinalPrice);
        }

        public async Task<int> GetMonthlyLessonCountAsync(int year, int month)
        {
            return await _context.Set<Lesson>()
                .Where(l => l.Date.Year == year && l.Date.Month == month)
                .CountAsync();
        }

        public async Task<List<decimal>> GetYearlyIncomeTrendAsync(int year)
        {
            var monthlyIncomes = new decimal[12];

            var incomes = await _context.Set<Lesson>()
                .Where(l => l.Date.Year == year && l.IsPaid)
                .GroupBy(l => l.Date.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(l => l.FinalPrice) })
                .ToListAsync();

            foreach (var income in incomes)
            {
                monthlyIncomes[income.Month - 1] = income.Total;
            }

            return [.. monthlyIncomes];
        }

        public async Task<Dictionary<string, decimal>> GetTopPayersThisMonthAsync(int year, int month, int limit = 5)
        {
            var groupedData = await _context.Set<Lesson>()
                .Where(l => l.Date.Year == year && l.Date.Month == month && l.IsPaid)
                .Include(l => l.Student)
                .ThenInclude(s => s.Payer)
                .GroupBy(l => l.Student.Payer)
                .Select(g => new
                {
                    PayerName = g.Key.FirstName + " " + g.Key.LastName,
                    TotalSpent = g.Sum(l => l.FinalPrice)
                })
                .ToListAsync();

            return groupedData
                .OrderByDescending(x => x.TotalSpent)
                .Take(limit)
                .ToDictionary(x => x.PayerName, x => x.TotalSpent);
        }

        public async Task<(int Paid, int Unpaid)> GetPaidVsUnpaidCountThisMonthAsync(int year, int month)
        {
            var data = await _context.Set<Lesson>()
                .Where(l => l.Date.Year == year && l.Date.Month == month)
                .GroupBy(l => l.IsPaid)
                .Select(g => new { IsPaid = g.Key, Count = g.Count() })
                .ToListAsync();

            int paid = data.FirstOrDefault(x => x.IsPaid)?.Count ?? 0;
            int unpaid = data.FirstOrDefault(x => !x.IsPaid)?.Count ?? 0;

            return (paid, unpaid);
        }
    }
}
