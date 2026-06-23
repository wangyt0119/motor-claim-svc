using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motor.Claim.Application.Dtos.Coverage;
using Motor.Claim.Application.Dtos.DamageAssessment;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Entities;
using Motor.Claim.WebApi.Models;
using Motor.Claim.WebApi.Services;

namespace Motor.Claim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "CustomerOnly")]
    public class DamageAssessmentController : ControllerBase
    {
        private static readonly HashSet<string> AllowedImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private readonly CoverageService _coverageService;
        private readonly IGeminiDamageAssessmentService _geminiDamageAssessmentService;

        public DamageAssessmentController(
            CoverageService coverageService,
            IGeminiDamageAssessmentService geminiDamageAssessmentService)
        {
            _coverageService = coverageService;
            _geminiDamageAssessmentService = geminiDamageAssessmentService;
        }

        [HttpPost("assess")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Assess([FromForm] DamageAssessmentFormRequest request, CancellationToken cancellationToken)
        {
            if (request.Image == null || request.Image.Length == 0)
            {
                return BadRequest("Damage image is required.");
            }

            if (!AllowedImageMimeTypes.Contains(request.Image.ContentType))
            {
                return BadRequest("Only JPEG, PNG, and WEBP images are supported.");
            }

            var userId = GetCurrentUserId();
            var coverages = await _coverageService.GetByUserIdAsync(userId);
            var eligibility = FindEligibleCoverage(request, coverages);

            if (!eligibility.IsEligible || eligibility.CoverageEntity == null)
            {
                return Ok(new DamageAssessmentResponse
                {
                    CoverageEligibility = new DamageAssessmentCoverageEligibilityResponse
                    {
                        IsEligible = false,
                        Message = eligibility.Message,
                        Coverage = eligibility.CoverageEntity == null ? null : MapCoverage(eligibility.CoverageEntity)
                    },
                    DamageSummary = "Coverage eligibility failed, so AI repair quotation was not generated.",
                    Severity = "Unknown",
                    Disclaimer = "Please select or add an active coverage that matches this vehicle before requesting an AI-assisted quotation."
                });
            }

            byte[] imageBytes;
            await using (var stream = request.Image.OpenReadStream())
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream, cancellationToken);
                imageBytes = memoryStream.ToArray();
            }

            try
            {
                var result = await _geminiDamageAssessmentService.AssessAsync(new GeminiDamageAssessmentInput
                {
                    ImageBytes = imageBytes,
                    ImageMimeType = request.Image.ContentType,
                    Coverage = eligibility.CoverageEntity,
                    CustomerMessage = request.CustomerMessage
                }, cancellationToken);

                return Ok(result);
            }
            catch (GeminiDamageAssessmentException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        message = "Damage assessment is temporarily unavailable. Please try again shortly."
                    });
            }
        }

        private static CoverageEligibilityResult FindEligibleCoverage(
            DamageAssessmentFormRequest request,
            List<CoverageEntity> coverages)
        {
            var candidates = coverages.AsEnumerable();
            if (request.CoverageId.HasValue)
            {
                candidates = candidates.Where(x => x.CoverageId == request.CoverageId.Value);
            }

            var vehicleMatches = candidates
                .Where(x =>
                    IsSame(x.VehicleMake, request.VehicleMake) &&
                    IsSame(x.VehicleModel, request.VehicleModel) &&
                    x.Year == request.Year &&
                    IsSame(x.ModelType, request.ModelType))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            if (!vehicleMatches.Any())
            {
                return new CoverageEligibilityResult
                {
                    IsEligible = false,
                    Message = "No coverage was found for the submitted vehicle make, model, year, and model type."
                };
            }

            var today = DateTime.Today;
            var activeCoverage = vehicleMatches.FirstOrDefault(x => x.EffectiveDate.Date <= today && x.ExpiryDate.Date >= today);
            if (activeCoverage == null)
            {
                return new CoverageEligibilityResult
                {
                    IsEligible = false,
                    Message = "The matching coverage is not active today.",
                    CoverageEntity = vehicleMatches.First()
                };
            }

            if (activeCoverage.RemainingCoverageAmount <= 0m)
            {
                return new CoverageEligibilityResult
                {
                    IsEligible = false,
                    Message = "The matching coverage has no remaining comprehensive coverage amount for AI vehicle damage assessment.",
                    CoverageEntity = activeCoverage
                };
            }

            return new CoverageEligibilityResult
            {
                IsEligible = true,
                Message = "Coverage is active and matches the submitted vehicle details.",
                CoverageEntity = activeCoverage
            };
        }

        private static bool IsSame(string? left, string? right)
        {
            return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string? value)
        {
            return string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static CoverageResponse MapCoverage(CoverageEntity coverage)
        {
            return new CoverageResponse
            {
                CoverageId = coverage.CoverageId,
                CreatedAt = coverage.CreatedAt,
                UserId = coverage.UserId,
                InsuredPersonName = coverage.InsuredPersonName,
                VehicleNo = coverage.VehicleNo,
                VehicleMake = coverage.VehicleMake,
                VehicleModel = coverage.VehicleModel,
                Year = coverage.Year,
                ModelType = coverage.ModelType,
                CoverageType = coverage.CoverageType,
                AuthorizedDriver = coverage.AuthorizedDriver,
                EffectiveDate = coverage.EffectiveDate,
                ExpiryDate = coverage.ExpiryDate,
                CoverageLimitAmount = coverage.CoverageLimitAmount,
                UsedClaimAmount = coverage.UsedClaimAmount,
                RemainingCoverageAmount = coverage.RemainingCoverageAmount,
                WindscreenCoverageLimitAmount = coverage.WindscreenCoverageLimitAmount,
                WindscreenUsedClaimAmount = coverage.WindscreenUsedClaimAmount,
                WindscreenRemainingCoverageAmount = coverage.WindscreenRemainingCoverageAmount
            };
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing UserId claim.");
            }

            return userId;
        }

        private class CoverageEligibilityResult
        {
            public bool IsEligible { get; set; }
            public string Message { get; set; } = string.Empty;
            public CoverageEntity? CoverageEntity { get; set; }
        }
    }
}
