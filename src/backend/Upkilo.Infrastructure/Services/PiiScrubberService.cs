using Upkilo.Core.Interfaces;
using Upkilo.Core.Helpers;

namespace Upkilo.Infrastructure.Services;

public class PiiScrubberService : IPiiScrubberService
{
    public string Scrub(string input)
    {
        return PiiHelper.RedactPii(input);
    }
}
