using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
        InvoiceRepository _invoiceRepository;
        PayerRepository _payerRepository;

        public ObservableCollection<PayerOption> Payers { get; } = new();
        public ObservableCollection<InvoiceAttendanceSummary> Attendances { get; } = new();

        [ObservableProperty] private Guid? selectedPayerId;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        public InvoicesViewModel(InvoiceRepository invoiceRepository, PayerRepository payerRepository)
        {
            _invoiceRepository = invoiceRepository;
            _payerRepository = payerRepository;

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
                var payers = await _payerRepository.GetPayerOptionsAsync();

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
                var data = await _invoiceRepository.GetInvoiceAttendancesAsync(SelectedPayerId.Value);

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
                await _invoiceRepository.UpdateAttendancesAsync(ids);

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
        public void BuildInvoicePdf(
            string invoiceName,
            DateOnly invoiceDate,
            UserProfile user,
            PayerSummary payer,
            ReadOnlySpan<InvoiceAttendanceSummary> attendances,
            string file)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var culture = new CultureInfo("es-ES");

            var list = attendances.ToArray();
            decimal subTotal = list.Sum(a => a.Price);
            decimal ivaPercent = (decimal)user.IvaPercent;
            decimal ivaAmount = Math.Round(subTotal * ivaPercent / 100m, 2, MidpointRounding.AwayFromZero);
            var total = subTotal + ivaAmount;

            var dateText = invoiceDate.ToString("dd'/'MM'/'yyyy");

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(11));


                    page.Header().PaddingBottom(20).Row(row =>
                    {
                        row.RelativeItem().Text("INVOICE").SemiBold().FontSize(24).FontColor(Colors.Blue.Medium);

                        row.ConstantItem(220).Column(col =>
                        {
                            col.Item().AlignRight().Text($"Date: {dateText}");
                            col.Item().AlignRight().Text($"Invoice: {invoiceName}").SemiBold();
                        });
                    });

                    page.Content().Column(col =>
                    {
                        // User & payer blocks
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Border(1).Padding(8).Column(c =>
                            {
                                c.Item().Text("From").SemiBold();
                                c.Item().Text(user.FullName);
                                c.Item().Text(user.Address);
                                c.Item().Text($"{user.ZipCode} {user.City}");
                                c.Item().Text($"Phone: {user.Phone}");
                                c.Item().Text($"CIF: {user.TaxId}");
                                c.Item().Text(user.Email);
                            });

                            r.ConstantItem(16); // spacer

                            r.RelativeItem().Border(1).Padding(8).Column(c =>
                            {
                                c.Item().Text("Invoice to:").SemiBold();
                                c.Item().Text(payer.FullName);
                                if (!string.IsNullOrWhiteSpace(payer.Address))
                                    c.Item().Text(payer.Address);
                                if (!string.IsNullOrWhiteSpace(payer.Zip) || !string.IsNullOrWhiteSpace(payer.City))
                                    c.Item().Text($"{payer.Zip} {payer.City}".Trim());
                                if (!string.IsNullOrWhiteSpace(payer.TaxId))
                                    c.Item().Text($"CIF/NIF: {user.TaxId}");
                            });
                        });

                        col.Item().Height(15);

                        // Attendances table

                        col.Item().Table(t =>
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
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Date").SemiBold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Lesson");
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Student");
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Price");
                            });

                            foreach (var a in list)
                            {
                                t.Cell().PaddingVertical(4).Text(a.Date.ToString("dd-MM-yyyy"));
                                t.Cell().PaddingVertical(4).Text(a.LessonName);
                                t.Cell().PaddingVertical(4).Text(a.StudentName);
                                t.Cell().PaddingVertical(4).AlignRight().Text(a.Price.ToString("C", culture));
                            }

                        });

                        col.Item().Height(15);

                        // Totals
                        col.Item().Row(r =>
                        {
                            r.RelativeItem(); // left empty

                            r.ConstantItem(260).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.ConstantColumn(110);
                                });

                                t.Cell().AlignRight().Text($"SUBTOTAL:");
                                t.Cell().AlignRight().Text( subTotal.ToString("C", culture));
                                t.Cell().AlignRight().Text("IVA*:");
                                t.Cell().AlignRight().Text($"{ivaPercent:0}%");
                                t.Cell().AlignRight().Text($"IVA amount:");
                                t.Cell().AlignRight().Text(ivaAmount.ToString("C", culture));
                                t.Cell().AlignRight().PaddingBottom(8).Text($"TOTAL:").SemiBold();
                                t.Cell().AlignRight().Text(total.ToString("C", culture)).SemiBold();
                            });
                        });

                        col.Item().Height(15);

                        col.Item().Text("*Factura exenta de IVA Artículo 20 Uno 10º de la Ley 37/1992 de 28 de diciembre del Impuesto sobre el Valor Añadido.")
                        .Italic().FontSize(10);

                        col.Item().Height(20);

                        col.Item().Border(1).Padding(8).Column(c =>
                        {
                            c.Item().Text("Payment").SemiBold();
                            c.Item().Text($"Bank transfer to: {user.FullName}");
                            c.Item().Text(user.BankName);
                            c.Item().Text(user.BankAccount);
                        });

                    });

                    page.Footer().PaddingTop(20).AlignCenter().Text($"In case of any clarification, please contact {user.Email}")
                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });

            doc.GeneratePdf(file);
        }

        [RelayCommand]
        public async Task LoadByInvoiceAsync(string? invoiceName)
        {
            if (string.IsNullOrWhiteSpace(invoiceName)) return;
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var data = await _invoiceRepository.GetInvoiceAttendancesAsync(invoiceName);

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

                SelectedPayerId = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }

        }

        public async Task<(int invoiceId, string InvoiceName)> CreateAndPersistInvoiceAsync(
            Guid payerId, IEnumerable<Guid> attendanceIds, string? requestedName)
            => await _invoiceRepository.CreateInvoiceAsync(payerId, attendanceIds, requestedName);

        public async Task<PayerSummary> GetPayer(Guid payerId)
            => await _payerRepository.GetPayerSummaryNoOutstandingAsync(payerId);

    }
}
