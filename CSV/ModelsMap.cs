using CsvHelper.Configuration;
using Models;
using System.Globalization;

namespace CSV
{
    public class ServiceMap : ClassMap<Service>
    {
        public ServiceMap()
        {
            Map(x => x.Id).Name("Id");
            Map(x => x.Name).Name("Name");
            Map(x => x.Price).Name("Price");
            Map(x => x.IsPricePerHour).Name("IsPricePerHour");
        }
    }
    public class PayerMap : ClassMap<Payer>
    {
        public PayerMap()
        {
            Map(x => x.Id).Name("Id");
            Map(x => x.FirstName).Name("FirstName");
            Map(x => x.LastName).Name("LastName");
            Map(x => x.Address).Name("Address");
            Map(x => x.ZipCode).Name("ZipCode");
            Map(x => x.City).Name("City");
            Map(x => x.TaxId).Name("TaxId");
        }
    }

    public class StudentMap : ClassMap<Student>
    {
        public StudentMap()
        {
            Map(x => x.Id).Name("Id");
            Map(x => x.FirstName).Name("FirstName");
            Map(x => x.LastName).Name("LastName");
            Map(x => x.PayerId).Name("PayerId");
        }
    }

    public class SpecificationMap : ClassMap<Specification>
    {
        public SpecificationMap()
        {
            Map(x => x.Id).Name("Id");
            Map(x => x.Name).Name("Name");
            Map(x => x.DurationMinutes).Name("DurationMinutes");
            Map(x => x.Price).Name("Price");
            Map(x => x.IsOnline).Name("IsOnline");
            Map(x => x.IsWeekendOrHoliday).Name("IsWeekendOrHoliday");
            Map(x => x.UsageCount).Name("UsageCount");
            Map(x => x.StudentId).Name("StudentId");
            Map(x => x.ServiceId).Name("ServiceId");
        }
    }

    public class LessonData
    {
        public Guid Id { get; set; }

        public DateOnly Date { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal FinalPrice { get; set; }
        public bool IsPaid { get; set; }
        public Guid StudentId { get; set; }
        public Guid? BillingDocumentId { get; set; }
        public bool IsPricePerHour { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsOnline { get; set; }
        public decimal TravelAllowance { get; set; }
        public bool IsWeekendOrHoliday { get; set; }
        public decimal WeekendFee { get; set; }
        public decimal Tip { get; set; }
        public string? Notes { get; set; }
    }

    public class LessonMap : ClassMap<LessonData>
    {
        public LessonMap()
        {
            Map(x => x.Id).Name("Id");
            Map(x => x.Date).Name("Date").TypeConverterOption.Format("dd-MM-yyyy");
            Map(x => x.Name).Name("Name");
            Map(x => x.FinalPrice).Name("FinalPrice");
            Map(x => x.IsPaid).Name("IsPaid");
            Map(x => x.Notes).Name("Notes");
            Map(x => x.IsPricePerHour).Name("IsPricePerHour");
            Map(x => x.DurationMinutes).Name("DurationMinutes");
            Map(x => x.BasePrice).Name("BasePrice");
            Map(x => x.IsOnline).Name("IsOnline");
            Map(x => x.TravelAllowance).Name("TravelAllowance");
            Map(x => x.IsWeekendOrHoliday).Name("IsWeekendOrHoliday");
            Map(x => x.WeekendFee).Name("WeekendFee");
            Map(x => x.Tip).Name("Tip");
            Map(x => x.StudentId).Name("StudentId");
            Map(x => x.BillingDocumentId).Name("BillId");
        }
    }

    public class BillData
    {
        public Guid Id { get; set; }
        public DocumentType Type { get; set; }
        public int SequenceNumber { get; set; }
        public DateTime CreatedUTC { get; set; }
        public string DocumentNumber = string.Empty;
        public Guid PayerId { get; set; }
    }

    public class BillMap : ClassMap<BillData>
    {
        public BillMap()
        {
            Map(x => x.Id).Name("Id");
            Map(x => x.DocumentNumber).Name("DocumentNumber");
            Map(x => x.Type).Name("Type");
            Map(x => x.CreatedUTC).Name("CreatedUTC").TypeConverterOption.Format("dd-MM-yyyy HH:mm:ss")
                .TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture); ;
            Map(x => x.PayerId).Name("PayerId");
            Map(x => x.SequenceNumber).Name("SequenceNumber");
        }
    }
}
