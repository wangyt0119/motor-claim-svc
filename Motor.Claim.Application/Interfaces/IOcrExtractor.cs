using Motor.Claim.Application.Dtos.Stp;

namespace Motor.Claim.Application.Interfaces
{
    public interface IOcrExtractor
    {
        Task<OcrExtractionResult> ExtractIdentityDocumentAsync(string frontPath, string? backPath);
        Task<OcrExtractionResult> ExtractDrivingLicenseAsync(string frontPath, string? backPath);
        Task<OcrExtractionResult> ExtractVehicleOwnershipCertificateAsync(string filePath);
        Task<OcrExtractionResult> ExtractPoliceReportAsync(string filePath);
    }
}
