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
    public partial class InvoiceLine(LessonLine lesson) : ObservableObject
    {
        public LessonLine Data { get; set; } = lesson;

        [ObservableProperty] private bool isSelected;

        public void RefreshDataUI()
        {
            OnPropertyChanged(nameof(Data));
        }
    }
    public partial class BillingViewModel : UserProfileViewModel
    {
        readonly IBillingRepository _billingRepository;
        readonly IPayerRepository _payerRepository;
        readonly ILessonRepository _lessonRepository;
        readonly PDF.IWriter _pdfWriter;

        public ObservableCollection<PayerOption> Payers { get; } = [];
        public ObservableCollection<InvoiceLine> Lessons { get; } = [];
        public ObservableCollection<BillingDocument> BillSuggestions { get; } = []; 

        [ObservableProperty] private Guid? selectedPayerId;
        [ObservableProperty] private string? searchBillText;
        [ObservableProperty] private decimal totalSelected;
        [ObservableProperty] private decimal totalAll;
        [ObservableProperty] private bool editMode;
        [ObservableProperty] private BillSummary bill;
        [ObservableProperty] private string? lastGeneratedFolder;

        public static BillSummary ResetBill() => new(null, Guid.NewGuid(), DocumentType.Ticket, 0, "", new DateTime());

        public BillingViewModel(IBillingRepository billingRepository, IPayerRepository payerRepository,  
            IUserProfileService userProfile, PDF.IWriter pdfWriter, ILessonRepository lessonRepository)
            : base(userProfile)
        {
            _billingRepository = billingRepository;
            _payerRepository = payerRepository;
            _pdfWriter = pdfWriter;

            EditMode = false;
            Bill = ResetBill();

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
            _lessonRepository = lessonRepository;
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

        private async Task UpdatePayerOptions()
        {
            // Update payers options
            var payers = await _payerRepository.GetPayerOptionsByUnbilledLessons();

            Payers.Clear();
            foreach (var p in payers) Payers.Add(p);
        }

        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load payers while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            await UpdatePayerOptions();

            SetExitFunction();
        }

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
                SetExitFunction("No payer was selected", InfoBarType.Warning);
                return;
            }

            var unbilledLessons = await _billingRepository.GetUnbilledLessonsAsync(SelectedPayerId.Value);

            Lessons.Clear();
            foreach (var l in unbilledLessons) Lessons.Add(new InvoiceLine(l));

            EditMode = false;
            
            SetExitFunction($"Loaded {Lessons.Count} lessons unbilled and unpaid", InfoBarType.Success);
        }

        public async Task LoadBillLessonsAsync()
        {
            if (IsBusy)
            {
                SetExitFunction($"Can't load {Bill.Name} lessons while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (Bill.Id is null)
            {
                SetExitFunction("No bill was selected", InfoBarType.Warning);
                return;
            }

            var billedLessons = await _billingRepository.GetLessonsFromBillAsync(Bill.Id.Value);

            Lessons.Clear();
            foreach (var l in billedLessons) Lessons.Add(new InvoiceLine(l));

            EditMode = true;

            SetExitFunction($"Loaded {Lessons.Count} lessons.", InfoBarType.Success);
        }

        public async Task MarkSelectedPaymentAsync(bool markAsPaid)
        {
            string actionName = markAsPaid ? "paid" : "unpaid";
            if (IsBusy)
            {
                SetExitFunction($"Can't mark lessons as {actionName} while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var ids = Lessons.Where(l => l.IsSelected).Select(l => l.Data.Id).ToList();

            if (ids.Count == 0)
            {
                SetExitFunction($"Please, select first a lesson to mark them as {actionName}.", InfoBarType.Info);
                return;
            }

            try
            {
                await _lessonRepository.UpdateLessonsPayment(ids, isPaid: markAsPaid);

                // Update UI
                for (int i = 0; i < Lessons.Count; i++)
                {
                    if (ids.Contains(Lessons[i].Data.Id))
                    {
                        Lessons[i].Data = Lessons[i].Data with { IsPaid = markAsPaid };
                        Lessons[i].RefreshDataUI();
                    }
                }

                await UpdatePayerOptions();

                SetExitFunction($"{ids.Count} were marked as {actionName} successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task RemoveSelectedLessonsAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't remove selected lessons while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var ids = Lessons.Where(l => l.IsSelected).Select(l => l.Data.Id).ToList();

            if (ids.Count == 0)
            {
                SetExitFunction($"Please, select first a lesson to remove it.", InfoBarType.Info);
                return;
            }

            try
            {
                await _lessonRepository.UnassignBillToLessons(ids);

                // Remove paid lessons from the UI
                for (int i = Lessons.Count - 1; i >= 0; i--)
                {
                    if (ids.Contains(Lessons[i].Data.Id))
                    {
                        Lessons.RemoveAt(i);
                    }
                }
                SetExitFunction($"{ids.Count} were removed from {Bill.Name}.", InfoBarType.Success);

            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task DeleteBillAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't remove bill document while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (Bill.Id is null)
            {
                SetExitFunction("Error, bill was not loaded properly.", InfoBarType.Error);
                return;
            }
            try
            {
                await _billingRepository.DeleteAsync(Bill.Id.Value);
                string deletedBillName = Bill.Name;
                EditMode = false;
                Lessons.Clear();
                SearchBillText = string.Empty;
                Bill = ResetBill();
                SetExitFunction($"Bill document '{deletedBillName}' deleted successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }

        }

        public async Task CreateBill(DocumentType type, DateTime date)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't generate invoice while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!Directory.Exists(Profile.BillingFolder))
            {
                SetExitFunction($"Directory '{Profile.BillingFolder}' does not exist.", InfoBarType.Error);
                return;
            }

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
                var document = await _billingRepository.CreateBillAsync(
                    SelectedPayerId.Value, lessonsIds, type, date);

                var filePath = Path.Combine(Profile.BillingFolder, $"{document.DocumentNumber}.pdf");

                Bill = new BillSummary(document.Id, document.PayerId, document.Type, document.SequenceNumber, document.DocumentNumber, document.CreatedUTC);

                string successMessage;
                if (type == DocumentType.Invoice)
                {
                    // Generate both invoice and ticket
                    _pdfWriter.GenerateInvoice(document.DocumentNumber, payer, lessons, Profile, filePath, Bill.Date);

                    var ticketFilePath = Path.Combine(Profile.BillingFolder, $"{document.DocumentNumber}-list.pdf");
                    _pdfWriter.GenerateTicket(document.DocumentNumber, payer, lessons, Profile, ticketFilePath, Bill.Date);

                    successMessage = $"Invoice and ticket saved:\n• {Path.GetFileName(filePath)}\n• {Path.GetFileName(ticketFilePath)}";
                }
                else
                {
                    // Generate only ticket
                    _pdfWriter.GenerateTicket(document.DocumentNumber, payer, lessons, Profile, filePath, Bill.Date);
                    successMessage = $"Ticket saved: {Path.GetFileName(filePath)}";
                }

                LastGeneratedFolder = Profile.BillingFolder;

                EditMode = true;
                SearchBillText = document.DocumentNumber;

                // Remove those not included in the invocie
                foreach (var item in Lessons.Where(l => !l.IsSelected).ToList())
                    Lessons.Remove(item);

                // Unselect lessons
                foreach (var item in Lessons)
                    item.IsSelected = false;

                await UpdatePayerOptions();

                SetExitFunction(successMessage, InfoBarType.Info);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        [RelayCommand]
        public async Task PrintDocument()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't print bill while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!Path.Exists(Profile.BillingFolder))
            {
                SetExitFunction("Billing folder does not exist, please configure it properly in the settings view.",
                    InfoBarType.Error);
                return;
            }

            var lessons = Lessons .Select(l => l.Data).ToList();
            var filePath = Path.Combine(Profile.BillingFolder, $"{Bill.Name}.pdf");
            var payer = await _payerRepository.GetPayerSummaryNoOutstandingAsync(Bill.PayerId);

            string successMessage;
            if (Bill.Type is DocumentType.Invoice)
            {
                // Generate both invoice and ticket
                _pdfWriter.GenerateInvoice(Bill.Name, payer, lessons, Profile, filePath, Bill.Date);

                var ticketFilePath = Path.Combine(Profile.BillingFolder, $"{Bill.Name}-list.pdf");
                _pdfWriter.GenerateTicket(Bill.Name, payer, lessons, Profile, ticketFilePath, Bill.Date);

                successMessage = $"Invoice and ticket saved:\n• {Path.GetFileName(filePath)}\n• {Path.GetFileName(ticketFilePath)}";
            }
            else
            {
                // Generate only ticket
                _pdfWriter.GenerateTicket(Bill.Name, payer, lessons, Profile, filePath, Bill.Date);
                successMessage = $"Ticket saved: {Path.GetFileName(filePath)}";
            }

            LastGeneratedFolder = Profile.BillingFolder;

            SetExitFunction(successMessage, InfoBarType.Info);
        }

        public async Task SuggestBills(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                BillSuggestions.Clear();
                return;
            }

            var matches = await _billingRepository.GetBillSuggestionsAsync(searchTerm);
            BillSuggestions.Clear();
            foreach (var match in matches) BillSuggestions.Add(match);
        }

        public void SelectBillToEdit(BillingDocument document)
        {
            Bill = new BillSummary(document.Id, document.PayerId, document.Type, document.SequenceNumber, document.DocumentNumber, 
                document.CreatedUTC);
            SearchBillText = document.DocumentNumber;
        }

        public async Task EditBill(DocumentType type, int sequence, DateTime newDate)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't edit bill while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (Bill.Id == null)
            {
                SetExitFunction("Can't edit bill if not loaded", InfoBarType.Warning);
                return;
            }

            try
            {
                var document = await _billingRepository.EditAsync(Bill.Id.Value, type, sequence, newDate);
                Bill = new BillSummary(document.Id, document.PayerId, document.Type, document.SequenceNumber, document.DocumentNumber,
                    document.CreatedUTC);
                SetExitFunction($"{Bill.Name} was edited successfully.", InfoBarType.Success);
            }
            catch (Exception ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        [RelayCommand]
        public void OpenBillingFolder()
        {
            if (string.IsNullOrWhiteSpace(LastGeneratedFolder) || !Directory.Exists(LastGeneratedFolder))
            {
                if (!string.IsNullOrWhiteSpace(Profile.BillingFolder) && Directory.Exists(Profile.BillingFolder))
                {
                    LastGeneratedFolder = Profile.BillingFolder;
                }
                else
                {
                    return;
                }
            }

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", LastGeneratedFolder);
            }
            catch
            {
                // Silently fail if we can't open the folder
            }
        }
    }
}
