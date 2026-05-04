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
        Task UpsertAsync(Payer payer);
        Task DeleteAsync(Guid id);
        Task UpdateAsync(Guid payerId, string firstName, string lastName, 
            string address, string zipCode, string city, string taxId);
    }

    public interface IStudentRepository
    {
        Task<IEnumerable<StudentSummary>> GetSudentsAsync();
        Task UpsertAsync(Student student);
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
    }

    public interface ILessonRepository
    {
        Task<IEnumerable<LessonSummary>> GetLessonsAsync(bool showOnlyUnpaid, int? months);
        Task AddLessonsAsync(List<Lesson> lessons);
        Task<Lesson> AddLessonAsync(DateOnly date, string name,
            bool isPricePerHour, int? duration, decimal pricePerStudent,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee,
             string? notes, IReadOnlyList<Guid> studentIds);
        Task<Lesson> UpdateLessonNoteAsync(Guid id, string? note);
        Task<Lesson> UpdateLesson(Guid id, DateOnly date, string name,
            bool isPricePerHour, int? duration, decimal pricePerStudent,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, string? note);
        Task<Lesson> AddAttendanceAsync(Guid lessonId, IReadOnlyCollection<Guid> studentIds);
        Task<Lesson> RemoveAttendanceAsync(Guid lessonId, Guid attendanceId);
        Task<Lesson> UpdateAttendanceAsync(Guid lessonId, Guid attendanceId, bool isPaid);
    }

    public interface IInvoiceRepository
    {
        Task<IEnumerable<InvoiceAttendanceSummary>> GetInvoiceAttendancesAsync(Guid payerId);
        Task<IEnumerable<InvoiceAttendanceSummary>> GetInvoiceAttendancesAsync(string invoiceName);
        Task UpdateAttendancesAsync(IEnumerable<Guid> attendancesIds);
        Task AddAsync(Invoice invoice);
        Task<(int invoiceId, string InvoiceName)> CreateInvoiceAsync(Guid payerId,
            IEnumerable<Guid> attendanceIds, string? requestedName);
        Task DeleteInvoiceAsync(int invoiceId);
        Task<IEnumerable<Invoice>> GetInvoicesAsync();
    }
}
