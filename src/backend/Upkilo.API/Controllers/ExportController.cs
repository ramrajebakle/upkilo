using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IInvoiceService _invoiceService;
    private readonly ITenantProvider _tenantProvider;

    public ExportController(IExportService exportService, IInvoiceService invoiceService, ITenantProvider tenantProvider)
    {
        _exportService = exportService;
        _invoiceService = invoiceService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet("clients")]
    public async Task<IActionResult> ExportClients()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var csvBytes = await _exportService.ExportClientsToCsvAsync(tenantId.Value);
        return File(csvBytes, "text/csv", $"clients_export_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> ExportBookings()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var csvBytes = await _exportService.ExportBookingsToCsvAsync(tenantId.Value);
        return File(csvBytes, "text/csv", $"bookings_export_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Download a specific invoice as PDF
    /// </summary>
    [HttpGet("invoices/{id}/pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Security check: If the user is a client, ensure they only download their own invoice
        var clientIdStr = User.FindFirst("client_id")?.Value;

        var invoice = await _invoiceService.GetInvoiceByIdAsync(id, tenantId.Value);
        if (invoice == null) return NotFound();

        if (!string.IsNullOrEmpty(clientIdStr) && Guid.TryParse(clientIdStr, out var clientId))
        {
            if (invoice.ClientId != clientId)
            {
                return Forbid("You do not have permission to download this invoice.");
            }
        }

        try
        {
            var pdfBytes = await _invoiceService.GenerateInvoicePdfAsync(id, tenantId.Value);
            return File(pdfBytes, "application/pdf", $"Invoice-{invoice.InvoiceNumber}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to generate PDF invoice.", details = ex.Message });
        }
    }
}

