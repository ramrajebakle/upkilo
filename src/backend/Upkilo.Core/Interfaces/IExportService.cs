using System.IO;
using System.Threading.Tasks;

namespace Upkilo.Core.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportClientsToCsvAsync(Guid tenantId);
    Task<byte[]> ExportBookingsToCsvAsync(Guid tenantId);
}
