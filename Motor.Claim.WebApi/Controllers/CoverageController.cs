using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motor.Claim.Application.Dtos.Claim;
using Motor.Claim.Application.Dtos.Coverage;
using Motor.Claim.Application.Features.Coverage.Commands;
using Motor.Claim.Application.Features.Coverage.Queries;

namespace Motor.Claim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "CustomerOnly")]
    public class CoverageController : ControllerBase
    {
        private readonly CreateCoverageCommandHandler _createCoverageCommandHandler;
        private readonly GetMyCoveragesQueryHandler _getMyCoveragesQueryHandler;

        public CoverageController(
            CreateCoverageCommandHandler createCoverageCommandHandler,
            GetMyCoveragesQueryHandler getMyCoveragesQueryHandler)
        {
            _createCoverageCommandHandler = createCoverageCommandHandler;
            _getMyCoveragesQueryHandler = getMyCoveragesQueryHandler;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCoverageRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                var command = new CreateCoverageCommand
                {
                    UserId = userId,
                    InsuredPersonName = request.InsuredPersonName,
                    VehicleNo = request.VehicleNo,
                    CoverageType = request.CoverageType,
                    EffectiveDate = request.EffectiveDate,
                    ExpiryDate = request.ExpiryDate
                };

                var result = await _createCoverageCommandHandler.Handle(command);
                return Ok(MapResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("my-coverages")]
        public async Task<IActionResult> GetMyCoverages()
        {
            var userId = GetCurrentUserId();

            var query = new GetMyCoveragesQuery
            {
                UserId = userId
            };

            var result = await _getMyCoveragesQueryHandler.Handle(query);
            return Ok(result.Select(MapResponse).ToList());
        }

        private static CoverageResponse MapResponse(Motor.Claim.Domain.Entities.CoverageEntity coverage)
        {
            return new CoverageResponse
            {
                CoverageId = coverage.CoverageId,
                CreatedAt = coverage.CreatedAt,
                UserId = coverage.UserId,
                InsuredPersonName = coverage.InsuredPersonName,
                VehicleNo = coverage.VehicleNo,
                CoverageType = coverage.CoverageType,
                EffectiveDate = coverage.EffectiveDate,
                ExpiryDate = coverage.ExpiryDate,
                Claims = coverage.Claims.Select(claim => new ClaimResponse
                {
                    ClaimId = claim.ClaimId,
                    UserId = claim.UserId,
                    CoverageId = claim.CoverageId,
                    IncidentDate = claim.IncidentDate,
                    CreatedAt = claim.CreatedAt,
                    AllClaimType = claim.AllClaimType,
                    MotorClaimType = claim.MotorClaimType,
                    IncidentDescription = claim.IncidentDescription,
                    PoliceReportDocument = claim.PoliceReportDocument,
                    VehicleOwnershipCertificateDocument = claim.VehicleOwnershipCertificateDocument,
                    IdentityDocumentFront = claim.IdentityDocumentFront,
                    IdentityDocumentBack = claim.IdentityDocumentBack,
                    DrivingLicenseFront = claim.DrivingLicenseFront,
                    DrivingLicenseBack = claim.DrivingLicenseBack,
                    VehicleDamageFrontLeftDocument = claim.VehicleDamageFrontLeftDocument,
                    VehicleDamageFrontRightDocument = claim.VehicleDamageFrontRightDocument,
                    VehicleDamageRearLeftDocument = claim.VehicleDamageRearLeftDocument,
                    VehicleDamageRearRightDocument = claim.VehicleDamageRearRightDocument,
                    Status = claim.Status
                }).ToList()
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
    }
}
