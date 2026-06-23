using Motor.Claim.Application.Dtos.DamageAssessment;

namespace Motor.Claim.Application.Interfaces
{
    public interface IGeminiDamageAssessmentService
    {
        Task<DamageAssessmentResponse> AssessAsync(GeminiDamageAssessmentInput input, CancellationToken cancellationToken = default);
    }
}
