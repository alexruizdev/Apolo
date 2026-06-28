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

        protected static string Message_State_Error => "Messages/State_Bill_Error";
        protected static string Message_Load_Error => "Messages/Load_Bill_Error";
        protected static string Message_Load_Lessons_Error => "Messages/Load_Lessons_Error";
        protected static string Message_Payer_Selected_Reason => "Messages/Payer_Selected_Reason";
        protected static string Message_Load_Lessons_Success => "Messages/Load_Lessons_Success";
        protected static string Message_Load_Bill_Lessons_Error => "Messages/Load_Bill_Lessons_Error";
        protected static string Message_Bill_Selected_Reason => "Messages/Bill_Selected_Reason";
        protected static string Message_Load_Bill_Lessons_Success => "Messages/Load_Bill_Lessons_Success";
        protected static string Message_Remove_Bill_Lessons_Error => "Messages/Remove_Bill_Lessons_Error";
        protected static string Message_Lessons_Selected_Reason => "Messages/Lessons_Selected_Reason";
        protected static string Message_Remove_Bill_Lessons_Success => "Messages/Remove_Bill_Lessons_Success";
        protected static string Message_Delete_Bill_Error => "Messages/Delete_Bill_Error";
        protected static string Message_Delete_Bill_Success => "Messages/Delete_Bill_Success";
        protected static string Message_Generate_Bill_Error => "Messages/Generate_Bill_Error";
        protected static string Message_Generate_Bill_Success => "Messages/Generate_Bill_Success";
        protected static string Message_Print_Bill_Error => "Messages/Print_Bill_Error";

        public static BillSummary ResetBill() => new(null, Guid.NewGuid(), DocumentType.Ticket, "", "");

        public BillingViewModel(IBillingRepository billingRepository, IPayerRepository payerRepository,  
            IUserProfileService userProfile, PDF.IWriter pdfWriter, ILessonRepository lessonRepository,
            IStringLocalizer stringLocalizer)
            : base(userProfile, stringLocalizer)
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
                    SetExitBusy(Message_State_Error);
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
                SetExitBusy(Message_Load_Error);
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
                SetExitBusy(Message_Load_Lessons_Error);
                return;
            }

            SetEnterFunction();

            if (SelectedPayerId is null)
            {
                SetExitFunction($"{_loc.Get(Message_Load_Lessons_Error)}: {_loc.Get(Message_Payer_Selected_Reason)}.",
                    InfoBarType.Warning);
                return;
            }

            var unbilledLessons = await _billingRepository.GetUnbilledLessonsAsync(SelectedPayerId.Value);

            Lessons.Clear();
            foreach (var l in unbilledLessons) Lessons.Add(new InvoiceLine(l));

            EditMode = false;
            
            SetExitFunction($"{_loc.Get(Message_Load_Lessons_Success, Lessons.Count)}.", InfoBarType.Success);
        }

        public async Task LoadBillLessonsAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Load_Bill_Lessons_Error);
                return;
            }

            SetEnterFunction();

            if (Bill.Id is null)
            {
                SetExitFunction($"{_loc.Get(Message_Load_Bill_Lessons_Error)}: {_loc.Get(Message_Bill_Selected_Reason)}.",
                    InfoBarType.Warning);
                return;
            }

            var billedLessons = await _billingRepository.GetLessonsFromBillAsync(Bill.Id.Value);

            Lessons.Clear();
            foreach (var l in billedLessons) Lessons.Add(new InvoiceLine(l));

            EditMode = true;

            SetExitFunction($"{_loc.Get(Message_Load_Bill_Lessons_Success, Bill.Name, Lessons.Count)}.", InfoBarType.Success);
        }

        public async Task MarkSelectedPaymentAsync(bool markAsPaid)
        {
            string actionName = markAsPaid ? "paid" : "unpaid";
            if (IsBusy)
            {
                SetExitBusy(Message_Change_Payment_Error);
                return;
            }

            SetEnterFunction();

            var ids = Lessons.Where(l => l.IsSelected).Select(l => l.Data.Id).ToList();

            if (ids.Count == 0)
            {
                if (markAsPaid)
                    SetExitFunction($"{_loc.Get(Message_Mark_Paid)}.", InfoBarType.Info);
                else
                    SetExitFunction($"{_loc.Get(Message_Mark_Unpaid)}.", InfoBarType.Info);
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
                
                if (markAsPaid)
                    SetExitFunction($"{_loc.Get(Message_Lessons_Mark_Paid, ids.Count)}.", InfoBarType.Success);
                else
                    SetExitFunction($"{_loc.Get(Message_Lessons_Mark_Unpaid, ids.Count)}.", InfoBarType.Success);
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
                SetExitBusy(Message_Remove_Bill_Lessons_Error);
                return;
            }

            SetEnterFunction();

            var ids = Lessons.Where(l => l.IsSelected).Select(l => l.Data.Id).ToList();

            if (ids.Count == 0)
            {
                SetExitFunction($"{_loc.Get(Message_Remove_Bill_Lessons_Error)}: {_loc.Get(Message_Lessons_Selected_Reason)}.",
                    InfoBarType.Info);
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
                SetExitFunction($"{_loc.Get(Message_Remove_Bill_Lessons_Success, ids.Count, Bill.Name)}.", InfoBarType.Success);

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
                SetExitBusy(Message_Delete_Bill_Error);
                return;
            }

            SetEnterFunction();

            if (Bill.Id is null)
            {
                SetExitFunction($"{_loc.Get(Message_Delete_Bill_Error)}: {_loc.Get(Message_Bill_NotLoaded, "")}.",
                    InfoBarType.Warning);
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
                SetExitFunction($"{_loc.Get(Message_Delete_Bill_Success, deletedBillName)}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }

        }

        public async Task GenerateInvoice(bool isInvoice)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Generate_Bill_Error);
                return;
            }

            SetEnterFunction();

            if (!Directory.Exists(Profile.BillingFolder))
            {
                SetExitFunction($"{_loc.Get(Message_Generate_Bill_Error)}: {_loc.Get(Message_Bill_Folder_Reason)}.",
                    InfoBarType.Error);
                return;
            }

            if (SelectedPayerId is null)
            {
                SetExitFunction($"{_loc.Get(Message_Generate_Bill_Error)}: {_loc.Get(Message_Payer_Selected_Reason)}.",
                    InfoBarType.Warning);
                return;
            }

            var lessons = Lessons
                .Where(l => l.IsSelected)
                .Select(l => l.Data)
                .ToList();

            if (lessons.Count == 0)
            {
                SetExitFunction($"{_loc.Get(Message_Generate_Bill_Error)}: {_loc.Get(Message_Lessons_Selected_Reason)}.",
                    InfoBarType.Warning);
                return;
            }

            var payer = await _payerRepository.GetPayerSummaryNoOutstandingAsync(SelectedPayerId.Value);
            var lessonsIds = lessons.Select(a => a.Id).ToList();

            try
            {
                var document = await _billingRepository.CreateBillAsync(
                    SelectedPayerId.Value, lessonsIds, isInvoice ? DocumentType.Invoice : DocumentType.Ticket);

                var filePath = Path.Combine(Profile.BillingFolder, $"{document.DocumentNumber}.pdf");

                var dateText = document.CreatedUTC.ToString("dd/MM/yyyy");

                if (isInvoice)
                    _pdfWriter.GenerateInvoice(document.DocumentNumber, payer, lessons, Profile, filePath, dateText);
                else
                    _pdfWriter.GenerateTicket(document.DocumentNumber, payer, lessons, Profile, filePath, dateText);

                Bill = new BillSummary(document.Id, document.PayerId, document.Type, document.DocumentNumber, dateText);
                EditMode = true;
                SearchBillText = document.DocumentNumber;

                // Remove those not included in the invocie
                foreach (var item in Lessons.Where(l => !l.IsSelected).ToList())
                    Lessons.Remove(item);

                // Unselect lessons
                foreach (var item in Lessons)
                    item.IsSelected = false;

                await UpdatePayerOptions();

                var documentName = (isInvoice ? DocumentType.Invoice : DocumentType.Ticket).ToString();
                SetExitFunction($"{_loc.Get(Message_Generate_Bill_Success, documentName, filePath)}.", InfoBarType.Info);
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
                SetExitBusy(Message_Print_Bill_Error);
                return;
            }

            SetEnterFunction();

            if (!Path.Exists(Profile.BillingFolder))
            {
                SetExitFunction($"{_loc.Get(Message_Print_Bill_Error)}: {_loc.Get(Message_Bill_Folder_Reason)}.",
                    InfoBarType.Error);
                return;
            }

            var lessons = Lessons .Select(l => l.Data).ToList();
            var filePath = Path.Combine(Profile.BillingFolder, $"{Bill.Name}.pdf");
            var payer = await _payerRepository.GetPayerSummaryNoOutstandingAsync(Bill.PayerId);

            if (Bill.Type is DocumentType.Invoice)
                _pdfWriter.GenerateInvoice(Bill.Name, payer, lessons, Profile, filePath, Bill.Date);
            else
                _pdfWriter.GenerateTicket(Bill.Name, payer, lessons, Profile, filePath, Bill.Date);

            SetExitFunction($"{_loc.Get(Message_Generate_Bill_Success, Bill.Name, filePath)}.", InfoBarType.Info);
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
            Bill = new BillSummary(document.Id, document.PayerId, document.Type, document.DocumentNumber, 
                document.CreatedUTC.ToString("dd/MM/yyyy"));
            SearchBillText = document.DocumentNumber;
        }
    }
}
