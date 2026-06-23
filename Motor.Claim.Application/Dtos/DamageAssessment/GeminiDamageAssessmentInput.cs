using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Dtos.DamageAssessment
{
    public class GeminiDamageAssessmentInput
    {
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
        public string ImageMimeType { get; set; } = string.Empty;
        public CoverageEntity Coverage { get; set; } = null!;
        public string? CustomerMessage { get; set; }
    }
}
