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
            return await _context.Lessons
                .Where(l => !l.IsPaid)
                .SumAsync(l => l.FinalPrice);
        }

        public async Task<decimal> GetMonthlyEarningsAsync(int year, int month)
        {
            var databaseResult = await _context.Lessons
                .Where(l => l.Date.Year == year && l.Date.Month == month && l.IsPaid)
                .SumAsync(l => l.FinalPrice);
            var archiveResult = await _archiveDb.Lessons
                .Where(l => l.Date.Year == year && l.Date.Month == month && l.IsPaid)
                .SumAsync(l => l.FinalPrice);
            return databaseResult + archiveResult;
        }

        public async Task<int> GetMonthlyLessonCountAsync(int year, int month)
        {
            var databaseCount = await _context.Lessons
                .Where(l => l.Date.Year == year && l.Date.Month == month)
                .CountAsync(); 
            var archiveCount = await _archiveDb.Lessons
                .Where(l => l.Date.Year == year && l.Date.Month == month)
                .CountAsync();
            return databaseCount + archiveCount;
        }

        public async Task<List<decimal>> GetYearlyIncomeTrendAsync(int year)
        {
            var monthlyIncomes = new decimal[12];

            var incomes = await _context.Lessons
                .Where(l => l.Date.Year == year && l.IsPaid)
                .GroupBy(l => l.Date.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(l => l.FinalPrice) })
                .ToListAsync();

            var archiveIncomes = await _archiveDb.Lessons
                .Where(l => l.Date.Year == year && l.IsPaid)
                .GroupBy(l => l.Date.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(l => l.FinalPrice) })
                .ToListAsync();

            var combinedIncomes = incomes
                .Concat(archiveIncomes)
                .GroupBy(x => x.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Sum(x => x.Total)
                })
                .OrderBy(x => x.Month) // Optional: keeps months 1 to 12 in order
                .ToList();

            foreach (var income in combinedIncomes)
            {
                monthlyIncomes[income.Month - 1] = income.Total;
            }

            return [.. monthlyIncomes];
        }

        public async Task<Dictionary<string, decimal>> GetTopPayersThisMonthAsync(int year, int month, int limit = 5)
        {
            // Fetch from live database
            var liveData = await _context.Lessons
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

            // Fetch from archive database
            var archiveData = await _archiveDb.Lessons
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

            // Merge collections, combine totals by payer name, order by highest spending, and take the limit
            return liveData
                .Concat(archiveData)
                .GroupBy(x => x.PayerName)
                .Select(g => new
                {
                    PayerName = g.Key,
                    TotalSpent = g.Sum(x => x.TotalSpent)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(limit)
                .ToDictionary(x => x.PayerName, x => x.TotalSpent);
        }

        public async Task<(int Paid, int Unpaid)> GetPaidVsUnpaidCountThisMonthAsync(int year, int month)
        {
            // Fetch counts from live database
            var liveData = await _context.Lessons
                .Where(l => l.Date.Year == year && l.Date.Month == month)
                .GroupBy(l => l.IsPaid)
                .Select(g => new { IsPaid = g.Key, Count = g.Count() })
                .ToListAsync();

            // Fetch counts from archive database
            var archiveData = await _archiveDb.Lessons
                .Where(l => l.Date.Year == year && l.Date.Month == month)
                .GroupBy(l => l.IsPaid)
                .Select(g => new { IsPaid = g.Key, Count = g.Count() })
                .ToListAsync();

            // Merge both in-memory datasets together
            var combinedData = liveData
                .Concat(archiveData)
                .GroupBy(x => x.IsPaid)
                .Select(g => new { IsPaid = g.Key, Count = g.Sum(x => x.Count) })
                .ToList();

            // Extract totals safely
            int paid = combinedData.FirstOrDefault(x => x.IsPaid)?.Count ?? 0;
            int unpaid = combinedData.FirstOrDefault(x => !x.IsPaid)?.Count ?? 0;

            return (paid, unpaid);
        }
    }
}
