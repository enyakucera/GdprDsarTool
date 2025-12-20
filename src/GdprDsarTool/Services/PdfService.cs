using GdprDsarTool.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GdprDsarTool.Services;

public class PdfService : IPdfService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PdfService> _logger;

    public PdfService(IWebHostEnvironment env, ILogger<PdfService> logger)
    {
        _env = env;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> GenerateDsarResponsePdfAsync(DsarRequest request)
    {
        try
        {
            var fileName = $"DSAR_Response_{request.Id}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            var pdfPath = Path.Combine(_env.WebRootPath, "pdfs", fileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);

            // Generate PDF
            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .Text($"GDPR Data Subject Access Request Response")
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(col =>
                            {
                                col.Spacing(20);

                                col.Item().Text($"Request ID: {request.Id}");
                                col.Item().Text($"Date: {DateTime.UtcNow:dd MMMM yyyy}");
                                col.Item().Text($"Request Type: {request.RequestType}");

                                col.Item().LineHorizontal(1);

                                col.Item().Text("Requester Information").SemiBold().FontSize(14);
                                col.Item().Text($"Name: {request.RequesterName}");
                                col.Item().Text($"Email: {request.RequesterEmail}");

                                col.Item().LineHorizontal(1);

                                col.Item().Text("Personal Data We Hold").SemiBold().FontSize(14);
                                col.Item().Text("Based on your request, we have identified the following personal data:");

                                // Mock data for prototype
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(5);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Data Type");
                                        header.Cell().Element(CellStyle).Text("Value");
                                    });

                                    table.Cell().Element(CellStyle).Text("Email");
                                    table.Cell().Element(CellStyle).Text(request.RequesterEmail);

                                    table.Cell().Element(CellStyle).Text("Name");
                                    table.Cell().Element(CellStyle).Text(request.RequesterName);

                                    table.Cell().Element(CellStyle).Text("Account Created");
                                    table.Cell().Element(CellStyle).Text("2023-01-15");

                                    table.Cell().Element(CellStyle).Text("Last Login");
                                    table.Cell().Element(CellStyle).Text("2024-12-10");
                                });

                                col.Item().LineHorizontal(1);

                                col.Item().Text("Your Rights").SemiBold().FontSize(14);
                                col.Item().Text("Under GDPR, you have the following rights:");
                                col.Item().Text("• Right to access your data");
                                col.Item().Text("• Right to rectification");
                                col.Item().Text("• Right to erasure");
                                col.Item().Text("• Right to data portability");
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Generated on ");
                                x.Span(DateTime.UtcNow.ToString("dd MMMM yyyy HH:mm")).SemiBold();
                            });
                    });
                })
                .GeneratePdf(pdfPath);
            });

            _logger.LogInformation($"PDF generated successfully: {fileName}");

            return $"/pdfs/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for request {RequestId}", request.Id);
            throw;
        }
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
    }
}
