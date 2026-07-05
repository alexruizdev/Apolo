using Models;

namespace Repository
{
    public interface IServiceRepository
    {
        Task<IEnumerable<ServiceSummary>> GetServicesAsync();
        Task AddAsync(Service entity);
        Task DeleteAsync(Guid id);
        Task UpdateAsync(Guid id, string name, bool isPricePerHour, decimal price);
    }

    public interface IPayerRepository
    {
        Task<PayerSummary> GetPayerSummaryNoOutstandingAsync(Guid payerId);
        Task<IEnumerable<PayerSummary>> GetPayersAsync();
        Task<IEnumerable<PayerOption>> GetPayerOptionsAsync();
        Task<IEnumerable<PayerOption>> GetPayerOptionsByUnbilledLessons();
        Task AddAsync(Payer payer);
        Task DeleteAsync(Guid id);
        Task UpdateAsync(Guid payerId, string firstName, string lastName, 
            string address, string zipCode, string city, string taxId);
    }

    public interface IStudentRepository
    {
        Task<IEnumerable<StudentSummary>> GetSudentsAsync();
        Task AddAsync(Student student);
        Task DeleteAsync(Guid id);
        Task UpdateAsync(Guid studentId, Guid payerId, string firstName, string lastName);
        Task<IEnumerable<StudentOption>> GetStudentOptionsAsync();
    }

    public interface ISpecificationRepository
    {
        Task AddSpecificationAsync(Specification specification);
        Task<IEnumerable<SpecificationSummary>> GetSpecificationsAsync();
        Task<IEnumerable<SpecificationOption>> GetSpecificationsForStudentAsync(IEnumerable<Guid> studentsIds);
        Task DeleteAsync(Guid id);
        Task UpdateAsync(Guid id, Guid serviceId, string name, int duration, decimal? price, bool isOnline, bool isWeekend);
        Task IncrementUsageAsync(Guid id);
    }

    public interface ILessonRepository
    {
        Task<IEnumerable<LessonSummary>> GetLessonsAsync(string studentName,
            string payerName,
            bool? isPaid, 
            DateOnly? startDate,
            DateOnly? endDate);
        Task<Lesson> AddLessonAsync(DateOnly date, string name, bool isPaid, Guid studentId,
            Guid? billingDocumentId, bool isPricePerHour, int? duration, decimal basePrice,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, decimal tip, string? notes);
        Task<Lesson> UpdateLesson(Guid id, DateOnly date, string name,
            bool isPricePerHour, int? duration, decimal pricePerStudent,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, decimal tip, string? note);
        Task UpdateLessonsPayment(IEnumerable<Guid> lessonsIds, bool isPaid);
        Task DeleteAsync(Guid id);
        Task UnassignBillToLessons(IEnumerable<Guid> lessonsIds);
    }

    public interface IBillingRepository
    {
        Task<IEnumerable<LessonLine>> GetUnbilledLessonsAsync(Guid payerId);
        Task<IEnumerable<LessonLine>> GetLessonsFromBillAsync(Guid billId);
        Task<BillingDocument> CreateBillAsync(Guid payerId, List<Guid> ids, DocumentType type, DateTime newDate);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<BillingDocument>> GetBillSuggestionsAsync(string searchTerm);
        Task<BillingDocument> EditAsync(Guid id, DocumentType type, int sequence, DateTime newDate);
    }

    public interface IGeneralRepository
    {
        Task ClearDatabaseAsync();
        Task ClearArchiveAsync();
        Task ImportAllDataAsync(
            List<Service> services,
            List<Payer> payers,
            List<Student> students,
            List<Specification> specifications,
            List<Lesson> lessons,
            List<BillingDocument> invoices);

        Task ImportArchiveAsync(
            List<Payer> payers,
            List<Student> students,
            List<Lesson> lessons,
            List<BillingDocument> invoices);

        Task<(List<Service> Services, List<Payer> Payers, List<Student> Students,
            List<Specification> Specifications, List<Lesson> Lessons, List<BillingDocument> Invoices)> GetAllDataAsync();
        Task<(List<Service> Services, List<Payer> Payers, List<Student> Students,
            List<Specification> Specifications, List<Lesson> Lessons, List<BillingDocument> Invoices)> ExportArchiveAsync();
        Task<List<PayerActivityInfo>> GetPayersWithActivityAsync();
        Task ArchiveOldDataAsync(List<Guid> payerIds);
        Task<List<PayerOption>> GetPayersFromArchiveAsync();
        Task RetrieveDataFromArchiveAsync(List<Guid> payerIds);
    }

    public interface IDashboardRepository
    {
        Task<decimal> GetTotalUnpaidAmountAsync();
        Task<decimal> GetMonthlyEarningsAsync(int year, int month);
        Task<int> GetMonthlyLessonCountAsync(int year, int month);
        Task<List<decimal>> GetYearlyIncomeTrendAsync(int year);
        Task<Dictionary<string, decimal>> GetTopPayersThisMonthAsync(int year, int month, int limit = 5);
        Task<(int Paid, int Unpaid)> GetPaidVsUnpaidCountThisMonthAsync(int year, int month);
    }
}
