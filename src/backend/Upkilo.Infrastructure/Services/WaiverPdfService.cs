using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class WaiverPdfService
{
    private readonly AppDbContext _context;
    private readonly ILogger<WaiverPdfService> _logger;

    public WaiverPdfService(AppDbContext context, ILogger<WaiverPdfService> logger)
    {
        _context = context;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateWaiverPdfAsync(Guid tenantId, Guid waiverSignatureId)
    {
        _logger.LogInformation("Generating waiver PDF for signature {Id}", waiverSignatureId);

        var signature = await _context.Set<WaiverSignature>()
            .Include(ws => ws.Waiver)
            .Include(ws => ws.Client)
            .FirstOrDefaultAsync(ws => ws.Id == waiverSignatureId && ws.Waiver!.TenantId == tenantId);

        if (signature == null)
            throw new InvalidOperationException($"Waiver signature {waiverSignatureId} not found.");

        var tenant = await _context.Tenants.FindAsync(tenantId);
        var tenantName = tenant?.Name ?? "Upkilo Business";

        var clientName = signature.Client != null
            ? $"{signature.Client.FirstName} {signature.Client.LastName}".Trim()
            : "Unknown Client";

        var clientEmail = signature.Client?.Email ?? string.Empty;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(c => ComposeHeader(c, tenantName));
                page.Content().Element(c => ComposeContent(c, signature, clientName, clientEmail));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();

        return pdfBytes;
    }

    private static void ComposeHeader(IContainer container, string tenantName)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text(tenantName).FontSize(20).Bold();
                    inner.Item().Text("Digital Waiver & Consent Form").FontSize(14).FontColor(Colors.Grey.Medium);
                });
                row.ConstantItem(120).AlignRight().Text(DateTime.Now.ToString("MMM dd, yyyy")).FontSize(10).FontColor(Colors.Grey.Medium);
            });
            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(IContainer container, WaiverSignature signature, string clientName, string clientEmail)
    {
        container.PaddingTop(20).Column(col =>
        {
            col.Item().Text(signature.Waiver?.Title ?? "Waiver").FontSize(16).Bold();

            col.Item().PaddingTop(10);

            if (!string.IsNullOrEmpty(signature.Waiver?.Content))
            {
                col.Item().Text(signature.Waiver.Content).FontSize(10).FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(20);
            }

            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().PaddingTop(10).Text("Signatory Information").FontSize(13).Bold();

            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Name: {clientName}");
                row.RelativeItem().Text($"Date: {signature.SignedAt:MMM dd, yyyy HH:mm} UTC");
            });

            if (!string.IsNullOrEmpty(clientEmail))
                col.Item().Text($"Email: {clientEmail}");

            col.Item().PaddingTop(20).Text("Electronic Signature").FontSize(13).Bold();

            col.Item().PaddingTop(5).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10)
                .Text("Digitally signed via Upkilo platform").Italic().FontColor(Colors.Grey.Medium);

            col.Item().PaddingTop(20).Text($"Document ID: {signature.Id}").FontSize(8).FontColor(Colors.Grey.Medium);
            col.Item().Text($"IP Address: {signature.SignedFromIP ?? "Not recorded"}").FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("This is a legally binding electronic signature document.").FontSize(8).FontColor(Colors.Grey.Medium);
                row.ConstantItem(100).AlignRight().Text(ctx =>
                {
                    ctx.CurrentPageNumber().Format(n => $"Page {n}");
                });
            });
        });
    }
}
