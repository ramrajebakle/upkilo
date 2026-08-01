using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Interfaces;
using Upkilo.Infrastructure.Helpers;

namespace Upkilo.Infrastructure.Templates;

/// <summary>
/// 80mm Thermal Receipt Template
/// </summary>
public class ThermalReceiptTemplate : IInvoiceTemplate
{
    public string TemplateName => "Thermal80mm";

    public void Compose(IDocumentContainer container, Invoice invoice, Tenant tenant, dynamic settings)
    {
        container.Page(page =>
        {
            // 80mm width, using points (1mm = 2.83465 points)
            // 80mm * 2.83465 = 226.77 points
            page.Size(226, 600, Unit.Point);
            page.Margin(10);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.CourierNew));

            page.Header().Column(col =>
            {
                col.Item().AlignCenter().Text(tenant.Name).Bold().FontSize(12);

                if (tenant.Settings.TryGetValue("CompanyAddress", out var addr) && addr is string address && !string.IsNullOrEmpty(address))
                    col.Item().AlignCenter().Text(address).FontSize(8);

                if (!string.IsNullOrEmpty(tenant.Phone))
                    col.Item().AlignCenter().Text(tenant.Phone).FontSize(8);

                if (tenant.Settings.TryGetValue("TaxId", out var tax) && tax is string taxId && !string.IsNullOrEmpty(taxId))
                    col.Item().AlignCenter().Text($"Tax ID: {taxId}").FontSize(8);

                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);

                col.Item().Text($"{InvoiceTranslator.GetLabel("Receipt", tenant.Locale)}: {invoice.InvoiceNumber}");
                col.Item().Text($"{InvoiceTranslator.GetLabel("Date", tenant.Locale)}: {invoice.IssueDate:g}");
                col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Black);
            });

            page.Content().Column(column =>
            {
                column.Spacing(2);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    foreach (var item in invoice.Items)
                    {
                        table.Cell().Text(item.Description);
                        table.Cell().AlignRight().Text($"{item.Quantity}x");
                        table.Cell().AlignRight().Text(Upkilo.Core.Helpers.Currency.Format(item.Amount, invoice.Currency));
                    }
                });

                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
                column.Item().AlignRight().Text($"{InvoiceTranslator.GetLabel("Total", tenant.Locale)}: {Upkilo.Core.Helpers.Currency.Format(invoice.TotalAmount, invoice.Currency)}").Bold().FontSize(11);

                // UPI QR Code
                if (tenant.Settings.TryGetValue("UpiId", out var upiIdObj) && upiIdObj is string upiId && !string.IsNullOrEmpty(upiId))
                {
                    column.Item().PaddingTop(10).AlignCenter().Column(qrCol =>
                    {
                        qrCol.Item().AlignCenter().Text(InvoiceTranslator.GetLabel("ScanToPay", tenant.Locale)).FontSize(8);

                        var qrBytes = PaymentQrHelper.GenerateUpiQrCode(
                            upiId,
                            tenant.Name,
                            invoice.TotalAmount,
                            invoice.Currency,
                            $"Receipt-{invoice.InvoiceNumber}"
                        );

                        qrCol.Item().PaddingTop(5).AlignCenter().Width(80).Image(qrBytes);
                    });
                }

                if (invoice.Status == InvoiceStatus.Paid)
                {
                    column.Item().AlignCenter().PaddingTop(10).Text(InvoiceTranslator.GetLabel("Paid", tenant.Locale)).Bold().FontSize(14).FontColor(Colors.Grey.Darken3);
                }
            });

            page.Footer().Column(col =>
            {
                if (tenant.Settings.TryGetValue("InvoiceFooterNote", out var note) && note is string footerNote && !string.IsNullOrEmpty(footerNote))
                    col.Item().PaddingTop(10).AlignCenter().Text(footerNote).FontSize(8).Italic();

                col.Item().PaddingTop(5).AlignCenter().Text(InvoiceTranslator.GetLabel("ThankYou", tenant.Locale));
                col.Item().AlignCenter().Text("www.upkilo.com");
            });
        });
    }
}
