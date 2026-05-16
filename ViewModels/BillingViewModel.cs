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
        public LessonLine Data { get; set; }

        [ObservableProperty] private bool isSelected;
        public InvoiceLine(LessonLine lesson)
        {
            this.Data = lesson;
        }
        
    }
    public partial class BillingViewModel : UserProfileViewModel
    {
        IBillingRepository _billingRepository;
        IPayerRepository _payerRepository;
        PDF.IWriter _pdfWriter;

        public ObservableCollection<PayerOption> Payers { get; } = new();
        public ObservableCollection<InvoiceLine> Lessons { get; } = new();

        [ObservableProperty] private Guid? selectedPayerId;
        [ObservableProperty] private decimal totalSelected;
        [ObservableProperty] private decimal totalAll;

        public BillingViewModel(IBillingRepository billingRepository, IPayerRepository payerRepository, 
            IUserProfileService userProfile, PDF.IWriter pdfWriter)
            : base(userProfile)
        {
            _billingRepository = billingRepository;
            _payerRepository = payerRepository;
            _pdfWriter = pdfWriter;

            Lessons.CollectionChanged += (s, e) =>
            {
                // 1. Wire up newly added items
                if (e.NewItems != null)
                {
                    foreach (InvoiceLine item in e.NewItems)
                        item.PropertyChanged += LessonOnPropertyChanged;
                }

                // 2. Unwire removed items to prevent memory leaks
                if (e.OldItems != null)
                {
                    foreach (InvoiceLine item in e.OldItems)
                        item.PropertyChanged -= LessonOnPropertyChanged;
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
                var selectedCount = Lessons.Count(a => a.IsSelected);

                if (selectedCount == 0) return false;
                if (selectedCount == Lessons.Count) return true;
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

                foreach (var a in Lessons)
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
            TotalSelected = Lessons.Where(a => a.IsSelected).Sum(l => l.Data.FinalPrice);
            TotalAll = Lessons.Sum(l => l.Data.FinalPrice);
            OnPropertyChanged(nameof(SelectionState));
        }

        private void LessonOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
        public async Task LoadLessonsAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load lessons while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (SelectedPayerId is null)
            {
                SetExitFunction();
                return;
            }

            var unbilledLessons = await _billingRepository.GetUnbilledLessonsAsync(SelectedPayerId.Value);

            Lessons.Clear();
            foreach (var l in unbilledLessons) Lessons.Add(new InvoiceLine(l));
            
            SetExitFunction();
        }

        [RelayCommand]
        public async Task MarkSelectedAsPaidAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't mark lessons as paid while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var ids = Lessons.Where(l => l.IsSelected).Select(l => l.Data.Id).ToList();

            if (ids.Count == 0)
            {
                SetExitFunction("Please, select first an lesson to mark them as paid.", InfoBarType.Info);
                return;
            }

            try
            {
                await _billingRepository.UpdateLessonsAsync(ids, isPaid: true);

                // Remove paid lessons from the UI
                for (int i = Lessons.Count - 1; i >= 0; i--)
                {
                    if (ids.Contains(Lessons[i].Data.Id))
                    {
                        Lessons.RemoveAt(i);
                    }
                }
                SetExitFunction($"{ids.Count} were marked as paid successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task GenerateInvoice(string folderPath, bool isInvoice)
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

            var lessons = Lessons
                .Where(l => l.IsSelected)
                .Select(l => l.Data)
                .ToList();

            if (lessons.Count == 0)
            {
                SetExitFunction("No lessons selected.", InfoBarType.Warning);
                return;
            }

            var payer = await _payerRepository.GetPayerSummaryNoOutstandingAsync(SelectedPayerId.Value);
            var lessonsIds = lessons.Select(a => a.Id).ToList();

            try
            {
                var documentNumber = await _billingRepository.CreateBillAsync(
                    SelectedPayerId.Value, lessonsIds, isInvoice ? DocumentType.Invoice : DocumentType.Ticket);

                var filePath = Path.Combine(folderPath, $"{documentNumber}.pdf");

                if (isInvoice)
                    _pdfWriter.GenerateInvoice(documentNumber, payer, lessons, Profile, filePath);
                else
                    _pdfWriter.GenerateTicket(documentNumber, payer, lessons, Profile, filePath);

                foreach (var item in Lessons.Where(l => l.IsSelected).ToList())
                    Lessons.Remove(item);

                var documentName = (isInvoice ? DocumentType.Invoice : DocumentType.Ticket).ToString();
                SetExitFunction($"{documentName} saved to: {filePath}.", InfoBarType.Info);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
                return;
            }

        }
    }
}
