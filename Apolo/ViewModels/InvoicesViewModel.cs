using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Repository;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Apolo.ViewModels
{
    public partial class InvoiceAttendanceSummary : ObservableObject
    {
        public Guid AttendanceId { get; }
        public Guid LessonId { get; }
        public DateOnly Date { get; }
        public string LessonName { get; }
        public Guid StudentId { get; }
        public string StudentName { get; }
        public decimal Price { get; }

        [ObservableProperty] private bool isSelected;
        public bool IsPaid => false;

        public InvoiceAttendanceSummary(
            Guid attendanceId,
            Guid lessonId,
            DateOnly date,
            string lessonName,
            Guid studentId,
            string studentName,
            decimal price
           )
        {
            AttendanceId = attendanceId;
            LessonId = lessonId;
            Date = date;
            LessonName = lessonName;
            StudentId = studentId;
            StudentName = studentName;
            Price = price;
        }
    }
    public partial class InvoicesViewModel : ObservableObject
    {
        InvoiceRepository _repository;

        public ObservableCollection<PayerOption> Payers { get; } = new();
        public ObservableCollection<InvoiceAttendanceSummary> Attendances { get; } = new();

        [ObservableProperty] private Guid? selectedPayerId;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        public InvoicesViewModel(InvoiceRepository repository)
        {
            _repository = repository;

            Attendances.CollectionChanged += (_, __) => RecomputeTotals();
        }

        // Tri-state "Select all" checkbox: true=all, false=none, null=mixed
        public bool? AllSelected
        {
            get
            {
                if (Attendances.Count == 0) return false;
                bool any = Attendances.Any(a => a.IsSelected);
                bool all = Attendances.All(a => a.IsSelected);
                if (!any) return false;
                return all ? true : null;
            }
            set
            {
                if (value is bool v) SetAllSelected(v);
            }
        }

        [ObservableProperty] private decimal totalSelected;
        [ObservableProperty] private decimal totalAll;

        private void RecomputeTotals()
        {
            TotalSelected = Attendances.Where(a => a.IsSelected).Sum(a => a.Price);
            TotalAll = Attendances.Sum(a => a.Price);
            OnPropertyChanged(nameof(AllSelected));
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var payers = await _repository.GetPayerOptionsAsync();

                Payers.Clear();
                foreach (var payer in payers) Payers.Add(payer);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task LoadAttendancesAsync()
        {
            if (SelectedPayerId is null) return;
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var data = await _repository.GetInvoiceAttendancesAsync(SelectedPayerId.Value);

                Attendances.Clear();
                foreach (var x in data)
                {
                    var invoice = new InvoiceAttendanceSummary(
                        x.AttendanceId,
                        x.LessonId,
                        x.Date,
                        x.LessonName,
                        x.StudentId,
                        x.StudentName,
                        x.Price);
                    invoice.PropertyChanged += AttendanceOnPropertyChanged;
                    Attendances.Add(invoice);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        private void AttendanceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InvoiceAttendanceSummary.IsSelected))
            {
                RecomputeTotals();
            }
        }

        private void SetAllSelected(bool value)
        {
            foreach (var a in Attendances) a.IsSelected = value;
        }

        [RelayCommand]
        public async Task MarkSelectedAsPaidAsync()
        {
            var ids = Attendances.Where(a => a.IsSelected).Select(a => a.AttendanceId).ToList();
            if (ids.Count == 0) return;
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await _repository.UpdateAttendancesAsync(ids);

                // Remove paid attendances from the UI
                for (int i = Attendances.Count - 1; i >= 0; i--)
                {
                    if (ids.Contains(Attendances[i].AttendanceId))
                        Attendances.RemoveAt(i);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        // PDF Generation (Quest PDF)
        // attendances must be the CURRENT selection you want to invoice
        public void BuildInvoicePdf(string payerName, ReadOnlySpan<InvoiceAttendanceSummary> attendances, string file)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var list = attendances.ToArray();
            var total = list.Sum(a => a.Price);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.Header().Text($"Invoice for {payerName}")
                        .SemiBold().FontSize(18);
                    page.Content().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(85); // Date
                            c.RelativeColumn(); // Lesson
                            c.RelativeColumn(); // Student
                            c.ConstantColumn(95); // Price
                        });

                        t.Header(h =>
                        {
                            h.Cell().Text("Date").SemiBold();
                            h.Cell().Text("Lesson");
                            h.Cell().Text("Student");
                            h.Cell().Text("Price");
                        });

                        foreach (var a in list)
                        {
                            t.Cell().Text(a.Date.ToString("dd-MM-yyyy"));
                            t.Cell().Text(a.LessonName);
                            t.Cell().Text(a.StudentName);
                            t.Cell().AlignRight().Text(a.Price.ToString("C"));
                        }

                        t.Cell().ColumnSpan(3).AlignRight().Text("Total").SemiBold();
                        t.Cell().AlignRight().Text(total.ToString("C")).SemiBold();
                    });

                    page.Footer().AlignRight().Text($"Generated {DateTime.Now:dd-MM-yyyy HH:mm}");
                });
            });

            doc.GeneratePdf(file);
        }
    }
}
