using System.Text;
using Upkilo.Core.Interfaces;
using System.Reflection;

namespace Upkilo.Infrastructure.Services;

public class CsvExportService : ICsvExportService
{
    public byte[] ExportToCsv<T>(IEnumerable<T> data)
    {
        var sb = new StringBuilder();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        // Header
        var headers = properties.Select(p => EscapeCsvField(p.Name));
        sb.AppendLine(string.Join(",", headers));

        // Data Rows
        foreach (var item in data)
        {
            if (item == null) continue;

            var fields = properties.Select(p => 
            {
                var val = p.GetValue(item, null);
                return EscapeCsvField(val?.ToString() ?? string.Empty);
            });
            sb.AppendLine(string.Join(",", fields));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        // Prevent CSV/Formula Injection
        if (field.StartsWith("=") || field.StartsWith("+") || field.StartsWith("-") || field.StartsWith("@"))
        {
            field = "\t" + field;
        }

        // If the field contains a comma, quote, or newline, wrap it in quotes and escape existing quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }
}
