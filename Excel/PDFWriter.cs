using Models;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace PDF
{
    public interface IWriter
    {
        public void GenerateInvoice(string invoiceName, PayerSummary payer, IEnumerable<LessonLine> lessons,
            UserProfile user, string filename, string dateText);
        public void GenerateTicket(string ticketName, PayerSummary payer, IEnumerable<LessonLine> lessons,
            UserProfile user, string filename, string dateText);
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
                    page.ContinuousSize(PageSizes.A5.Width);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().PaddingBottom(20).Row(row =>
                    {
                        row.ConstantItem(220).Column(col =>
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
                                h.Cell().AlignRight().Background(Colors.Grey.Lighten3).Padding(5).Text("Price").SemiBold();
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

                        // Simplified Totals
                        col.Item().Row(r =>
                        {
                            r.RelativeItem(); // left empty

                            r.ConstantItem(200).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.ConstantColumn(100);
                                });

                                // Made the total a bit larger to stand out since there are no subtotals
                                t.Cell().AlignRight().PaddingBottom(8).Text($"TOTAL:").SemiBold().FontSize(14);
                                t.Cell().AlignRight().Text(total.ToString("C", culture)).SemiBold().FontSize(14);
                            });
                        });
                    });
                });
            });

            doc.GeneratePdf(filename);
        }
    }
}
