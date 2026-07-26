using QuestPDF.Infrastructure;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Interfaces;

public interface IInvoiceTemplate
{
    string TemplateName { get; }
    void Compose(IDocumentContainer container, Invoice invoice, Tenant tenant, dynamic settings);
}
