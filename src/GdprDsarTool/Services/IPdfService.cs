using GdprDsarTool.Models;

namespace GdprDsarTool.Services;

public interface IPdfService
{
    Task<string> GenerateDsarResponsePdfAsync(DsarRequest request);
}
