using Motor.Claim.Application.Dtos.Stp;
using Motor.Claim.Application.Interfaces;

namespace Motor.Claim.Infrastructure.Shared.Services
{
    public class MockOcrExtractor : IOcrExtractor
    {
        public Task<OcrExtractionResult> ExtractDrivingLicenseAsync(string frontPath, string? backPath)
        {
            return Task.FromResult(BuildMockResult(frontPath));
        }

        public Task<OcrExtractionResult> ExtractIdentityDocumentAsync(string frontPath, string? backPath)
        {
            return Task.FromResult(BuildMockResult(frontPath));
        }

        public Task<OcrExtractionResult> ExtractPoliceReportAsync(string filePath)
        {
            return Task.FromResult(BuildMockResult(filePath));
        }

        public Task<OcrExtractionResult> ExtractVehicleOwnershipCertificateAsync(string filePath)
        {
            return Task.FromResult(BuildMockResult(filePath));
        }

        private static OcrExtractionResult BuildMockResult(string source)
        {
            var normalized = source.ToLowerInvariant();

            if (normalized.Contains("lowconfidence"))
            {
                return new OcrExtractionResult
                {
                    IsSuccess = true,
                    Confidence = 0.50m,
                    Name = "LOW CONFIDENCE",
                    ICNumber = "000000000000",
                    VehicleNumber = "LOW0000"
                };
            }

            if (normalized.Contains("fail"))
            {
                return new OcrExtractionResult
                {
                    IsSuccess = false,
                    Confidence = 0.10m,
                    ErrorMessage = "Mock OCR failure"
                };
            }

            return new OcrExtractionResult
            {
                IsSuccess = true,
                Confidence = 0.95m,
                Name = "SARAH",
                ICNumber = "030303030303",
                VehicleNumber = "ABC1234"
            };
        }
    }
}
