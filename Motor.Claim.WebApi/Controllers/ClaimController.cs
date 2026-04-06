using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motor.Claim.Application.Dtos.Claim;
using Motor.Claim.Application.Features.Claim.Commands;
using Motor.Claim.Application.Features.Claim.Queries;

namespace Motor.Claim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimController : ControllerBase
    {
        private readonly CreateClaimCommandHandler _createClaimCommandHandler;
        private readonly GetMyClaimsQueryHandler _getMyClaimsQueryHandler;
        private readonly GetAllClaimsQueryHandler _getAllClaimsQueryHandler;

        public ClaimController(
            CreateClaimCommandHandler createClaimCommandHandler,
            GetMyClaimsQueryHandler getMyClaimsQueryHandler,
            GetAllClaimsQueryHandler getAllClaimsQueryHandler)
        {
            _createClaimCommandHandler = createClaimCommandHandler;
            _getMyClaimsQueryHandler = getMyClaimsQueryHandler;
            _getAllClaimsQueryHandler = getAllClaimsQueryHandler;
        }

        [HttpPost]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> Create([FromBody] CreateClaimRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                var command = new CreateClaimCommand
                {
                    UserId = userId,
                    CoverageId = request.CoverageId,
                    IncidentDate = request.IncidentDate,
                    AllClaimType = request.AllClaimType,
                    MotorClaimType = request.MotorClaimType,
                    IncidentDescription = request.IncidentDescription,
                    PoliceReportDocument = request.PoliceReportDocument,
                    VehicleOwnershipCertificateDocument = request.VehicleOwnershipCertificateDocument,
                    IdentityDocumentFront = request.IdentityDocumentFront,
                    IdentityDocumentBack = request.IdentityDocumentBack,
                    DrivingLicenseFront = request.DrivingLicenseFront,
                    DrivingLicenseBack = request.DrivingLicenseBack,
                    VehicleDamageFrontLeftDocument = request.VehicleDamageFrontLeftDocument,
                    VehicleDamageFrontRightDocument = request.VehicleDamageFrontRightDocument,
                    VehicleDamageRearLeftDocument = request.VehicleDamageRearLeftDocument,
                    VehicleDamageRearRightDocument = request.VehicleDamageRearRightDocument
                };

                var result = await _createClaimCommandHandler.Handle(command);

                return Ok(MapResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("my-claims")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> GetMyClaims()
        {
            var userId = GetCurrentUserId();

            var query = new GetMyClaimsQuery
            {
                UserId = userId
            };

            var result = await _getMyClaimsQueryHandler.Handle(query);

            var response = result.Select(x => new ClaimResponse
            {
                ClaimId = x.ClaimId,
                UserId = x.UserId,
                CoverageId = x.CoverageId,
                IncidentDate = x.IncidentDate,
                CreatedAt = x.CreatedAt,
                AllClaimType = x.AllClaimType,
                MotorClaimType = x.MotorClaimType,
                IncidentDescription = x.IncidentDescription,
                PoliceReportDocument = x.PoliceReportDocument,
                VehicleOwnershipCertificateDocument = x.VehicleOwnershipCertificateDocument,
                IdentityDocumentFront = x.IdentityDocumentFront,
                IdentityDocumentBack = x.IdentityDocumentBack,
                DrivingLicenseFront = x.DrivingLicenseFront,
                DrivingLicenseBack = x.DrivingLicenseBack,
                VehicleDamageFrontLeftDocument = x.VehicleDamageFrontLeftDocument,
                VehicleDamageFrontRightDocument = x.VehicleDamageFrontRightDocument,
                VehicleDamageRearLeftDocument = x.VehicleDamageRearLeftDocument,
                VehicleDamageRearRightDocument = x.VehicleDamageRearRightDocument,
                Status = x.Status
            }).ToList();

            return Ok(response);
        }

        [HttpGet("all")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> GetAllClaims()
        {
            var result = await _getAllClaimsQueryHandler.Handle(new GetAllClaimsQuery());

            var response = result.Select(MapResponse).ToList();

            return Ok(response);
        }

        private static ClaimResponse MapResponse(Motor.Claim.Domain.Entities.ClaimEntity claim)
        {
            return new ClaimResponse
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
