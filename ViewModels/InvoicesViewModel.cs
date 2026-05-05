using Apolo.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ViewModels;

namespace Apolo.ViewModels
{
    public partial class InvoiceLine : ObservableObject
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

        public InvoiceLine(
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
    public partial class InvoicesViewModel : UserProfileViewModel
    {
        IInvoiceRepository _invoiceRepository;
        IPayerRepository _payerRepository;
        PDF.IWriter _pdfWriter;

        public ObservableCollection<PayerOption> Payers { get; } = new();
        public ObservableCollection<InvoiceLine> Attendances { get; } = new();

        [ObservableProperty] private Guid? selectedPayerId;
        [ObservableProperty] private decimal totalSelected;
        [ObservableProperty] private decimal totalAll;

        public InvoicesViewModel(IInvoiceRepository invoiceRepository, IPayerRepository payerRepository, 
            IUserProfileService userProfile, PDF.IWriter pdfWriter)
            : base(userProfile)
        {
            _invoiceRepository = invoiceRepository;
            _payerRepository = payerRepository;
            _pdfWriter = pdfWriter;

            Attendances.CollectionChanged += (s, e) =>
            {
                // 1. Wire up newly added items
                if (e.NewItems != null)
                {
                    foreach (InvoiceLine item in e.NewItems)
                        item.PropertyChanged += AttendanceOnPropertyChanged;
                }

                // 2. Unwire removed items to prevent memory leaks
                if (e.OldItems != null)
                {
                    foreach (InvoiceLine item in e.OldItems)
                        item.PropertyChanged -= AttendanceOnPropertyChanged;
                }

                // 3. Always recompute when the list structure changes
                RecomputeTotals();
            };
        }

        // Tri-state "Selection state" checkbox: true=all, false=none, null=mixed
        public bool? SelectionState
        {
            get
            {
                var selectedCount = Attendances.Count(a => a.IsSelected);

                if (selectedCount == 0) return false;
                if (selectedCount == Attendances.Count) return true;
                return null;
            }
            set
            {
                if (IsBusy)
                {
                    SetExitFunction("Can't update selection while busy.", InfoBarType.Warning, false);
                    return;
                }

                SetEnterFunction();

                if (!value.HasValue)
                {
                    SetExitFunction();
                    return;
                }

                foreach (var a in Attendances)
                {
                    // Only set if different to avoid unnecessary property change triggers
                    if (a.IsSelected != value)
                        a.IsSelected = value.Value;
                }

                SetExitFunction();
            }
        }

        private void RecomputeTotals()
        {
            TotalSelected = Attendances.Where(a => a.IsSelected).Sum(a => a.Price);
            TotalAll = Attendances.Sum(a => a.Price);
            OnPropertyChanged(nameof(SelectionState));
        }

        private void AttendanceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InvoiceLine.IsSelected))
            {
                RecomputeTotals();
            }
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load payers while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var payers = await _payerRepository.GetPayerOptionsAsync();

            Payers.Clear();
            foreach (var payer in payers) Payers.Add(payer);

            SetExitFunction();
        }

        [RelayCommand]
        public async Task LoadAttendancesAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load attendances while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (SelectedPayerId is null)
            {
                SetExitFunction();
                return;
            }

            var data = await _invoiceRepository.GetInvoiceAttendancesAsync(SelectedPayerId.Value);

            Attendances.Clear();
            foreach (var x in data)
            {
                var invoice = new InvoiceLine(
                    x.AttendanceId,
                    x.LessonId,
                    x.Date,
                    x.LessonName,
                    x.StudentId,
                    x.StudentName,
                    x.Price);
                Attendances.Add(invoice);
            }
            SetExitFunction();
        }

        [RelayCommand]
        public async Task MarkSelectedAsPaidAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't mark attendances as paid while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var ids = Attendances.Where(a => a.IsSelected).Select(a => a.AttendanceId).ToList();

            if (ids.Count == 0)
            {
                SetExitFunction("Please, select first an attendance to mark them as paid.", InfoBarType.Info);
                return;
            }

            try
            {
                await _invoiceRepository.UpdateAttendancesAsync(ids);

                // Remove paid attendances from the UI
                for (int i = Attendances.Count - 1; i >= 0; i--)
                {
                    if (ids.Contains(Attendances[i].AttendanceId))
                    {
                        Attendances.RemoveAt(i);
                    }
                }
                SetExitFunction($"{ids.Count} were marked as paid successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task GenerateInvoice(string folderPath, string? requestedName)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't generate invoice while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (SelectedPayerId is null)
            {
                SetExitFunction("No payer selected.", InfoBarType.Warning);
                return;
            }

            var attendances = Attendances
                .Where(a => a.IsSelected)
                .Select(a => new InvoiceAttendanceSummary(
                    a.AttendanceId, a.LessonId, a.Date, a.LessonName, a.StudentId, a.StudentName, a.Price))
                .ToList();
            if (attendances.Count == 0)
            {
                SetExitFunction("No attendances selected.", InfoBarType.Warning);
                return;
            }

            var payer = await _payerRepository.GetPayerSummaryNoOutstandingAsync(SelectedPayerId.Value);
            var attendanceIds = attendances.Select(a => a.AttendanceId).ToArray();

            int invoiceId = 0;
            string invoiceName = string.Empty;

            try
            {
                (invoiceId, invoiceName) = await _invoiceRepository.CreateInvoiceAsync(
                    SelectedPayerId.Value, attendanceIds, requestedName);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
                return;
            }

            var filePath = Path.Combine(folderPath, $"{invoiceName}.pdf");

            _pdfWriter.GenerateInvoice(invoiceName, payer, attendances, Profile, filePath);

            SetExitFunction($"Invoice saved to: {filePath}.", InfoBarType.Info);
        }
       

        [RelayCommand]
        public async Task LoadByInvoiceAsync(string? invoiceName)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load invoice while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (string.IsNullOrWhiteSpace(invoiceName))
            {
                SetExitFunction("Invoice name is required.", InfoBarType.Warning);
                return;
            }

            var data = await _invoiceRepository.GetInvoiceAttendancesAsync(invoiceName);

            Attendances.Clear();
            foreach (var x in data)
            {
                var invoice = new InvoiceLine(
                    x.AttendanceId,
                    x.LessonId,
                    x.Date,
                    x.LessonName,
                    x.StudentId,
                    x.StudentName,
                    x.Price);
                Attendances.Add(invoice);
            }

            SelectedPayerId = null;
            SetExitFunction();

        }
    }
}
