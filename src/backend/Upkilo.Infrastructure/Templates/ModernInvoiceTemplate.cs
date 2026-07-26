using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Interfaces;
using Upkilo.Infrastructure.Helpers;

namespace Upkilo.Infrastructure.Templates;

public class ModernInvoiceTemplate : IInvoiceTemplate
{
    public string TemplateName => "Modern";

    public void Compose(IDocumentContainer container, Invoice invoice, Tenant tenant, dynamic settings)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(tenant.Name).SemiBold().FontSize(20).FontColor(tenant.PrimaryColor ?? Colors.Blue.Medium);
                    
                    if (tenant.Settings.TryGetValue("CompanyAddress", out var addr) && addr is string address && !string.IsNullOrEmpty(address))
                        col.Item().Text(address).FontSize(9).FontColor(Colors.Grey.Medium);
                    
                    if (tenant.Settings.TryGetValue("TaxId", out var tax) && tax is string taxId && !string.IsNullOrEmpty(taxId))
                        col.Item().Text($"Tax ID: {taxId}").FontSize(9).FontColor(Colors.Grey.Medium);

                    col.Item().PaddingTop(5).Text($"{InvoiceTranslator.GetLabel("Invoice", tenant.Locale)} #{invoice.InvoiceNumber}");
                    col.Item().Text(invoice.IssueDate.ToString("d"));
                });

                if (!string.IsNullOrEmpty(tenant.LogoUrl))
                {
                    // For now, we use a placeholder that matches the brand color
                    row.ConstantItem(100).Height(50).Background(tenant.PrimaryColor ?? Colors.Blue.Medium).AlignCenter().AlignMiddle().Text("LOGO").FontColor(Colors.White).Bold();
                }
            });

            page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(10);

                // Industry Specific: Hospital
                if (invoice.Industry == "Hospital" && invoice.Metadata.ContainsKey("PatientName"))
                {
                    column.Item().Text($"{InvoiceTranslator.GetLabel("Patient", tenant.Locale)}: {invoice.Metadata["PatientName"]}").Italic();
                }

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(InvoiceTranslator.GetLabel("BillTo", tenant.Locale)).Bold().FontColor(tenant.PrimaryColor ?? Colors.Blue.Medium);
                        col.Item().Text(invoice.CustomerName);
                        col.Item().Text(invoice.CustomerEmail);
                        if (!string.IsNullOrEmpty(invoice.BillToAddress))
                            col.Item().Text(invoice.BillToAddress);
                    });
                });

                column.Item().LineHorizontal(1.5f).LineColor(tenant.PrimaryColor ?? Colors.Blue.Medium);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        var brandColor = tenant.PrimaryColor ?? Colors.Blue.Medium;
                        header.Cell().Element(CellStyle).Text("#");
                        header.Cell().Element(CellStyle).Text(InvoiceTranslator.GetLabel("Description", tenant.Locale));
                        header.Cell().Element(CellStyle).AlignRight().Text(InvoiceTranslator.GetLabel("UnitPrice", tenant.Locale));
                        header.Cell().Element(CellStyle).AlignRight().Text(InvoiceTranslator.GetLabel("Quantity", tenant.Locale));
                        header.Cell().Element(CellStyle).AlignRight().Text(InvoiceTranslator.GetLabel("Total", tenant.Locale));

                        IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold().FontColor(brandColor)).PaddingVertical(5).BorderBottom(1).BorderColor(brandColor);
                        }
                    });

                    int index = 1;
                    foreach (var item in invoice.Items)
                    {
                        table.Cell().Element(CellStyle).Text(index.ToString());
                        table.Cell().Element(CellStyle).Text(item.Description);
                        table.Cell().Element(CellStyle).AlignRight().Text(Upkilo.Core.Helpers.Currency.Format(item.UnitPrice, invoice.Currency));
                        table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                        table.Cell().Element(CellStyle).AlignRight().Text(Upkilo.Core.Helpers.Currency.Format(item.Amount, invoice.Currency));
                        index++;

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                        }
                    }
                });

                column.Item().AlignRight().Text($"{InvoiceTranslator.GetLabel("Total", tenant.Locale)}: {Upkilo.Core.Helpers.Currency.Format(invoice.TotalAmount, invoice.Currency)}").FontSize(14).SemiBold().FontColor(tenant.PrimaryColor ?? Colors.Blue.Medium);

                // UPI QR Code
                if (tenant.Settings.TryGetValue("UpiId", out var upiIdObj) && upiIdObj is string upiId && !string.IsNullOrEmpty(upiId))
                {
                    column.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(InvoiceTranslator.GetLabel("ScanToPay", tenant.Locale)).Bold();
                            col.Item().Text($"VPA: {upiId}");
                        });

                        var qrBytes = PaymentQrHelper.GenerateUpiQrCode(
                            upiId,
                            tenant.Name,
                            invoice.TotalAmount,
                            invoice.Currency,
                            $"Invoice-{invoice.InvoiceNumber}"
                        );

                        row.ConstantItem(80).Image(qrBytes);
                    });
                }
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
            });

            if (invoice.Status == InvoiceStatus.Paid)
            {
                page.Foreground().AlignCenter().AlignMiddle().Rotate(-45).Text(InvoiceTranslator.GetLabel("Paid", tenant.Locale))
                    .FontSize(100).FontColor(Colors.Red.Lighten3.WithAlpha(0.3f)).Bold();
            }
        });
    }
}
