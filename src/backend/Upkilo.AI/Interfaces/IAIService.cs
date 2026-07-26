using System.Threading.Tasks;
using System.Collections.Generic;

namespace Upkilo.AI.Interfaces;

public interface IAIService
{
    Task<string> GeneralQueryAsync(string prompt, string? model = null);
    Task<string> AnalyzeTextAsync(string text, string instruction);
    Task<IEnumerable<float>> GenerateEmbeddingsAsync(string text);
}
