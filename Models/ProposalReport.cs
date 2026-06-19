namespace Models
{
    public enum FrequencyUnit
    {
        PerWeek,
        PerMonth
    }

    public record ProposalInput
    {
        public string ServiceName { get; set; } = string.Empty;
        public double BasePrice { get; set; }
        public bool IsOnline { get; set; }
        public decimal TravelAllowance { get; set; }
        public bool IsWeekendOrHoliday { get; set; }
        public decimal WeekendFee { get; set; }
        public bool IsPricePerHour { get; set; }
        public double Duration { get; set; }
        public int Frequency { get; set; } = 1;
        public FrequencyUnit Unit { get; set; } = FrequencyUnit.PerWeek;
    }

    public record BudgetColumn
    {
        public string Label { get; set; } = string.Empty; // "Minus (-1)", "Requested", "Plus (+1)"
        public int Frequency { get; set; }
        public FrequencyUnit Unit { get; set; }
        public double SessionsPerMonth { get; set; }
        public decimal TotalPricePerMonth { get; set; }
    }

    public record ProposalReport
    {
        public string ServiceName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string RateMultiplier { get; set; } = "x1";
        public decimal Subtotal { get; set; }
        public double Duration { get; set; }
        public decimal WeekendFeeApplied { get; set; }
        public decimal TravelAllowanceApplied { get; set; }

        // Final calculations for requested setup
        public decimal PricePerSession { get; set; }
        public double SessionsPerMonth { get; set; }
        public decimal PricePerMonth { get; set; }

        // Alternative Modality Comparisons (Per Session)
        public BudgetColumn AlternativeTravel { get; set; } = new();
        public BudgetColumn AlternativeFee { get; set; } = new();

        // The 3 columns required: Minus, Requested, Plus
        public BudgetColumn BudgetMinus { get; set; } = new();
        public BudgetColumn BudgetRequested { get; set; } = new();
        public BudgetColumn BudgetPlus { get; set; } = new();
    }

    public static class ProposalService
    {
        private const double WeeksPerMonthMultiplier = 4.33;

        public static ProposalReport CalculateProposal(ProposalInput input)
        {
            var report = new ProposalReport
            {
                ServiceName = input.ServiceName,
                BasePrice = (decimal)input.BasePrice,
                Duration = input.Duration,
                WeekendFeeApplied = input.IsWeekendOrHoliday ? input.WeekendFee : 0m,
                TravelAllowanceApplied = input.IsOnline ? 0m : input.TravelAllowance
            };

            report.BasePrice += report.WeekendFeeApplied;

            // Calculate Single Session Price based on your rules:
            // Weekend Fee is added to the base price before multiplying by duration.
            report.PricePerSession = Lesson.GetPrice(input.IsOnline, input.TravelAllowance,
            input.IsWeekendOrHoliday, input.WeekendFee, (decimal)input.BasePrice, input.IsPricePerHour, (int)input.Duration);
            report.Subtotal = report.PricePerSession - report.TravelAllowanceApplied;
            report.RateMultiplier = Lesson.GetRate(input.IsPricePerHour, (int)input.Duration);

            // Calculate Frequency Multipliers
            report.SessionsPerMonth = CalculateSessionsPerMonth(input.Frequency, input.Unit);
            report.PricePerMonth = report.PricePerSession * (decimal)report.SessionsPerMonth;

            // Generate Cross-Modality Matrix Previews for request budget
            var alternativeTravelPrice = Lesson.GetPrice(isOnline: !input.IsOnline, input.TravelAllowance,
                input.IsWeekendOrHoliday, input.WeekendFee, (decimal)input.BasePrice, input.IsPricePerHour, (int)input.Duration);
            var alternativeTravelTitle = input.IsOnline ? "IN-PERSON" : "ONLINE";
            report.AlternativeTravel = BuildColumn($"{alternativeTravelTitle} OPTION", input.Frequency, input.Unit, alternativeTravelPrice);
            var alternativeFeePrice = Lesson.GetPrice(isOnline: input.IsOnline, input.TravelAllowance,
                !input.IsWeekendOrHoliday, input.WeekendFee, (decimal)input.BasePrice, input.IsPricePerHour, (int)input.Duration);
            var alternativeFeeTitle = input.IsWeekendOrHoliday ? "WEEK DAY" : "WEEKEND OR HOLIDAY";
            report.AlternativeFee = BuildColumn($"{alternativeFeeTitle} OPTION", input.Frequency, input.Unit, alternativeFeePrice);

            // Build the 3 Column Budgets (Minus 1, Requested, Plus 1)
            report.BudgetRequested = BuildColumn("REQUEST BUDGET", input.Frequency, input.Unit, report.PricePerSession);

            int minusFreq = Math.Max(1, input.Frequency - 1);
            report.BudgetMinus = BuildColumn($"REDUCED OPTIONS (-1)", minusFreq, input.Unit, report.PricePerSession);

            int plusFreq = input.Frequency + 1;
            report.BudgetPlus = BuildColumn($"EXPANDED OPTIONS (+1)", plusFreq, input.Unit, report.PricePerSession);

            return report;
        }

        private static double CalculateSessionsPerMonth(int frequency, FrequencyUnit unit)
        {
            return unit == FrequencyUnit.PerWeek
                ? frequency * WeeksPerMonthMultiplier
                : frequency;
        }

        private static BudgetColumn BuildColumn(string label, int freq, FrequencyUnit unit, decimal pricePerSession)
        {
            double sessions = CalculateSessionsPerMonth(freq, unit);
            return new BudgetColumn
            {
                Label = label,
                Frequency = freq,
                Unit = unit,
                SessionsPerMonth = sessions,
                TotalPricePerMonth = pricePerSession * (decimal)sessions
            };
        }
    }
}
