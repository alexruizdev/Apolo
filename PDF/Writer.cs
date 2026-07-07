using Models;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PDF
{
    public interface IReportWriter
    {
        public void GenerateProposal(string filename, ProposalReport report);
    }
    public interface IWriter
    {
        public void GenerateInvoice(string invoiceName, PayerSummary payer, IEnumerable<LessonLine> lessons,
            UserProfile user, string filename, string dateText);
        public void GenerateTicket(string ticketName, PayerSummary payer, IEnumerable<LessonLine> lessons,
            UserProfile user, string filename, string dateText);
    }

    public class ReportWriter() : IReportWriter
    {
        private ProposalReport _report = new();

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Service Proposal").FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(150).AlignRight().Column(column =>
                {
                    column.Item().Text("OFFICIAL QUOTATION").FontSize(12).Bold().FontColor(Colors.Grey.Darken2);
                    column.Item().Text($"Date: {System.DateTime.Today:dd/MM/yyyy}").FontSize(9);
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            var frequencyStr = _report.BudgetRequested.Unit == FrequencyUnit.PerWeek ? "week" : "month";
            container.PaddingTop(1, Unit.Centimetre).Column(column =>
            {
                // 1. Service Context Summary
                column.Item().Background(Colors.Grey.Lighten4).Padding(12).CornerRadius(4).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Target Service Summary").FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                        c.Item().Text(_report.ServiceName).FontSize(14).Bold();
                    });
                });

                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                // 2. Twin Columns: Left (Session breakdown table) | Right (Requested Monthly total)
                column.Item().Row(row =>
                {
                    // Left: Price Per Session Table
                    row.RelativeItem(1.1f).Column(c =>
                    {
                        c.Item().PaddingBottom(6).Text("Session Cost Breakdown").FontSize(11).Bold();

                        c.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).Row(r => {
                            r.RelativeItem().Text("Base Price");
                            r.ConstantItem(80).Text($"{_report.BasePrice:C}").AlignRight();
                        });

                        if (_report.WeekendFeeApplied > 0)
                        {
                            c.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).Row(r => {
                                r.RelativeItem().Text("Weekend / Holiday Fee");
                                r.ConstantItem(80).Text($"+ {_report.WeekendFeeApplied:C}").AlignRight().FontColor(Colors.Orange.Darken2);
                            });
                        }

                        if (_report.Duration > 1.0 || _report.WeekendFeeApplied > 0) // display rate mapping details
                        {
                            c.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).Row(r => {
                                r.RelativeItem().Text("Duration Multiplier");
                                r.ConstantItem(80).Text($"× {_report.RateMultiplier:F2} hrs").AlignRight();
                            });

                            c.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).Row(r => {
                                r.RelativeItem().Text("Subtotal").Bold();
                                r.ConstantItem(80).Text($"{_report.Subtotal:C}").AlignRight().Bold();
                            });
                        }

                        if (_report.TravelAllowanceApplied > 0)
                        {
                            c.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).Row(r => {
                                r.RelativeItem().Text("Travel Allowance (In-Person)");
                                r.ConstantItem(80).Text($"+ {_report.TravelAllowanceApplied:C}").AlignRight();
                            });
                        }

                        c.Item().Background(Colors.Green.Lighten5).Padding(6).Row(r => {
                            r.RelativeItem().Text("Final Session Cost").Bold().FontColor(Colors.Green.Darken3);
                            r.ConstantItem(80).Text($"{_report.PricePerSession:C}").AlignRight().Bold().FontColor(Colors.Green.Darken3);
                        });
                    });

                    row.ConstantItem(24); // Spacer

                    // Right: Requested Monthly Highlight Card
                    row.RelativeItem(0.9f).Background(Colors.Blue.Lighten5).Padding(16).CornerRadius(6).Column(c =>
                    {
                        c.Item().Text("REQUESTED PLAN TOTAL").FontSize(10).Bold().FontColor(Colors.Blue.Darken3);
                        c.Item().PaddingTop(4).Text($"{_report.BudgetRequested.TotalPricePerMonth:C}").FontSize(24).Bold().FontColor(Colors.Blue.Darken3);
                        c.Item().Text("estimated cost per month").FontSize(9).Italic().FontColor(Colors.Grey.Darken1);

                        c.Item().PaddingTop(12).BorderTop(1).BorderColor(Colors.Blue.Lighten3);
                        c.Item().PaddingTop(4).Text($"Frequency Context:").FontSize(9).Bold();
                        c.Item().Text($"{_report.BudgetRequested.Frequency} session(s) / {frequencyStr}").FontSize(10);
                        if (_report.BudgetRequested.Unit == FrequencyUnit.PerWeek)
                            c.Item().Text($"({_report.BudgetRequested.SessionsPerMonth:F1} total billable sessions/month)").FontSize(9).FontColor(Colors.Grey.Darken2);
                    });
                });

                column.Item().PaddingTop(1, Unit.Centimetre);

                // 3. Three-Column Budget Matrix Row
                column.Item().PaddingBottom(8).Text("Frequency Scaling Budgets").FontSize(12).Bold();
                column.Item().Row(row =>
                {
                    // Column Minus
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text(_report.BudgetMinus.Label).Bold().FontSize(10).FontColor(Colors.Orange.Darken3);
                        c.Item().PaddingTop(4).Text($"{_report.BudgetMinus.TotalPricePerMonth:C} /month").FontSize(13).Bold();
                        c.Item().Text($"{_report.BudgetMinus.Frequency} session(s) / {frequencyStr}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        if (_report.BudgetRequested.Unit == FrequencyUnit.PerWeek)
                            c.Item().Text($"({_report.BudgetMinus.SessionsPerMonth:F1} total/month)").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(12); // Grid margin spacing

                    // Column Requested
                    row.RelativeItem().Border(2).BorderColor(Colors.Blue.Darken1).Padding(10).Column(c =>
                    {
                        c.Item().Text("Target Setup").Bold().FontSize(10).FontColor(Colors.Blue.Darken2);
                        c.Item().PaddingTop(4).Text($"{_report.BudgetRequested.TotalPricePerMonth:C} /month").FontSize(13).Bold();
                        c.Item().Text($"{_report.BudgetRequested.Frequency} session(s) / {frequencyStr}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        if (_report.BudgetRequested.Unit == FrequencyUnit.PerWeek)
                            c.Item().Text($"({_report.BudgetRequested.SessionsPerMonth:F1} total/month)").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(12); // Grid margin spacing

                    // Column Plus
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text(_report.BudgetPlus.Label).Bold().FontSize(10).FontColor(Colors.Green.Darken3);
                        c.Item().PaddingTop(4).Text($"{_report.BudgetPlus.TotalPricePerMonth:C} /month").FontSize(13).Bold();
                        c.Item().Text($"{_report.BudgetPlus.Frequency} session(s) / {frequencyStr}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        if (_report.BudgetRequested.Unit == FrequencyUnit.PerWeek)
                            c.Item().Text($"({_report.BudgetPlus.SessionsPerMonth:F1} total/month)").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });

                column.Item().PaddingTop(1, Unit.Centimetre);

                // 4. Alternative Operational Frameworks (Cross Modality Previews)
                column.Item().PaddingBottom(6).Text("Alternative Operational Scenarios").FontSize(12).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(120);
                    });

                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Alternative Operational Setup").Bold().FontSize(10);
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Estimated Price/Month").Bold().FontSize(10).AlignRight();

                    // Row Modality Framework
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(_report.AlternativeTravel.Label);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text($"{_report.AlternativeTravel.TotalPricePerMonth:C}").AlignRight();

                    // Row Calendar Framework
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(_report.AlternativeFee.Label);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text($"{_report.AlternativeFee.TotalPricePerMonth:C}").AlignRight();
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text("This budget proposal is valid for 30 days from date of issue.")
                    .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                row.ConstantItem(100).AlignRight().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }

        public void GenerateProposal(string filename, ProposalReport report)
        {
            _report = report;
            QuestPDF.Settings.License = LicenseType.Community;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
            });
            doc.GeneratePdf(filename);
        }
    }
    public class Writer : IWriter
    {
        public void GenerateInvoice(string invoiceName, PayerSummary payer, IEnumerable<LessonLine> lessons, 
            UserProfile user, string filename, string dateText)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var culture = new CultureInfo("es-ES");

            var list = lessons.ToArray();
            decimal subTotal = list.Sum(l => l.FinalPrice);
            decimal ivaPercent = (decimal)user.IvaPercent;
            decimal ivaAmount = Math.Round(subTotal * ivaPercent / 100m, 2, MidpointRounding.AwayFromZero);
            var total = subTotal + ivaAmount;

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
                                c.Item().Text(payer.Name);
                                if (!string.IsNullOrWhiteSpace(payer.Address))
                                    c.Item().Text(payer.Address);
                                if (!string.IsNullOrWhiteSpace(payer.Zip) || !string.IsNullOrWhiteSpace(payer.City))
                                    c.Item().Text($"{payer.Zip} {payer.City}".Trim());
                                if (!string.IsNullOrWhiteSpace(payer.TaxId))
                                    c.Item().Text($"CIF/NIF: {user.TaxId}");
                            });
                        });

                        col.Item().Height(15);

                        // Lessons table

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

                            foreach (var l in list)
                            {
                                t.Cell().PaddingVertical(4).Text(l.Date.ToString("dd-MM-yyyy"));
                                t.Cell().PaddingVertical(4).Text(l.Name);
                                t.Cell().PaddingVertical(4).Text(l.StudentName);
                                t.Cell().PaddingVertical(4).AlignRight().Text(l.FinalPrice.ToString("C", culture));
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
                                t.Cell().AlignRight().Text(subTotal.ToString("C", culture));
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

            doc.GeneratePdf(filename);
        }

        public void GenerateTicket(string ticketName, PayerSummary payer, IEnumerable<LessonLine> lessons,
            UserProfile user, string filename, string dateText)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var culture = new CultureInfo("es-ES");

            var list = lessons.ToArray();
            decimal total = list.Sum(l => l.FinalPrice); // No taxes, just the direct sum

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.ContinuousSize(PageSizes.A4.Width);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().PaddingBottom(20).Row(row =>
                    {
                        row.ConstantItem(180).Column(col =>
                        {
                            col.Item().AlignRight().Text($"Date: {dateText}");
                        });
                    });

                    page.Content().Column(col =>
                    {
                        // Simplified Payer block (No 'From' section)
                        col.Item().Padding(8).Column(c =>
                        {
                            c.Item().Text("Class list").SemiBold();
                        });

                        col.Item().Height(15);

                        // Lessons table
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(90); // Date
                                c.RelativeColumn(2); // Lesson - more space
                                c.RelativeColumn(1.5f); // Student - good space
                                c.ConstantColumn(70); // Duration
                                c.ConstantColumn(85); // Price
                            });

                            t.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Date").SemiBold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Lesson").SemiBold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Student").SemiBold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Duration").SemiBold();
                                h.Cell().AlignRight().Background(Colors.Grey.Lighten3).Padding(5).Text("Price").SemiBold();
                            });

                            foreach (var l in list)
                            {
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(5).Text(l.Date.ToString("dd-MM-yyyy"));
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(5).Text(l.Name);
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(5).Text(l.StudentName);
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(5).Text(l.DurationMinutes.HasValue ? $"{l.DurationMinutes.Value} min" : "-");
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(5).AlignRight().Text(l.FinalPrice.ToString("C", culture));
                            }
                        });

                        col.Item().Height(20);

                        // Totals
                        col.Item().Row(r =>
                        {
                            r.RelativeItem(); // left empty

                            r.ConstantItem(220).Background(Colors.Grey.Lighten4).Padding(12).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.ConstantColumn(110);
                                });

                                t.Cell().AlignRight().Text($"TOTAL:").SemiBold().FontSize(15);
                                t.Cell().AlignRight().Text(total.ToString("C", culture)).SemiBold().FontSize(15).FontColor(Colors.Blue.Darken2);
                            });
                        });
                    });
                });
            });

            doc.GeneratePdf(filename);
        }

    }
}
