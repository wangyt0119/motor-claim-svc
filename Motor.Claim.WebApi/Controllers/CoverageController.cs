using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Motor.Claim.Application.Dtos.Claim;
using Motor.Claim.Application.Dtos.Coverage;
using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Features.Coverage.Commands;
using Motor.Claim.Application.Features.Coverage.Queries;

namespace Motor.Claim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoverageController : ControllerBase
    {
        private readonly CreateCoverageCommandHandler _createCoverageCommandHandler;
        private readonly GetMyCoveragesQueryHandler _getMyCoveragesQueryHandler;
        private readonly GetAllCoveragesQueryHandler _getAllCoveragesQueryHandler;

        public CoverageController(
            CreateCoverageCommandHandler createCoverageCommandHandler,
            GetMyCoveragesQueryHandler getMyCoveragesQueryHandler,
            GetAllCoveragesQueryHandler getAllCoveragesQueryHandler)
        {
            _createCoverageCommandHandler = createCoverageCommandHandler;
            _getMyCoveragesQueryHandler = getMyCoveragesQueryHandler;
            _getAllCoveragesQueryHandler = getAllCoveragesQueryHandler;
        }

        [HttpPost]
        [Authorize(Policy = "CustomerOnly")]
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
                    VehicleMake = request.VehicleMake,
                    VehicleModel = request.VehicleModel,
                    Year = request.Year,
                    ModelType = request.ModelType,
                    CoverageType = request.CoverageType,
                    AuthorizedDriver = request.AuthorizedDriver,
                    EffectiveDate = request.EffectiveDate,
                    ExpiryDate = request.ExpiryDate,
                    CoverageLimitAmount = request.CoverageLimitAmount,
                    WindscreenCoverageLimitAmount = request.WindscreenCoverageLimitAmount
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
        [Authorize(Policy = "CustomerOnly")]
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

        [HttpGet("all-coverage")]
        [HttpGet("all-coverages")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> GetAllCoverages()
        {
            var result = await _getAllCoveragesQueryHandler.Handle(new GetAllCoveragesQuery());
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
                WindscreenRemainingCoverageAmount = coverage.WindscreenRemainingCoverageAmount,
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
                    Status = claim.Status,
                    ReviewStatus = claim.ReviewStatus,
                    STPStatus = claim.STPStatus,
                    IsSTPApproved = claim.IsSTPApproved,
                    IsFlaggedForManualReview = claim.IsFlaggedForManualReview,
                    ManualReviewFlagReason = claim.ManualReviewFlagReason,
                    ValidationResult = claim.ValidationResult,
                    EmailNotificationSent = claim.EmailNotificationSent,
                    EmailNotificationMessage = claim.EmailNotificationMessage,
                    OfficerDecisionNote = claim.OfficerDecisionNote,
                    RequestedItems = claim.RequestedItems,
                    CustomerResponseNote = claim.CustomerResponseNote,
                    ResponseDocuments = DeserializeDocuments(claim.ResponseDocuments),
                    RequestedAt = claim.RequestedAt,
                    RespondedAt = claim.RespondedAt,
                    DecidedAt = claim.DecidedAt,
                    WithdrawnAt = claim.WithdrawnAt,
                    WithdrawalReason = claim.WithdrawalReason,
                    ReviewedByUserId = claim.ReviewedByUserId,
                    WorkshopAppointment = MapWorkshopAppointment(claim.WorkshopAppointment),
                    WorkshopRepairEstimate = MapWorkshopRepairEstimate(claim.WorkshopRepairEstimate),
                    WorkshopPayment = claim.WorkshopPayment == null ? null : Motor.Claim.Application.Services.WorkshopPaymentService.MapResponse(claim.WorkshopPayment)
                }).ToList()
            };
        }

        private static List<string> DeserializeDocuments(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(payload) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static WorkshopAppointmentResponse? MapWorkshopAppointment(Motor.Claim.Domain.Entities.WorkshopAppointmentEntity? appointment)
        {
            if (appointment?.Workshop == null)
            {
                return null;
            }

            return new WorkshopAppointmentResponse
            {
                AppointmentId = appointment.AppointmentId,
                ClaimId = appointment.ClaimId,
                WorkshopId = appointment.WorkshopId,
                WorkshopName = appointment.Workshop.Name,
                WorkshopState = appointment.Workshop.State,
                WorkshopAddress = appointment.Workshop.Address,
                PreferredDate = appointment.PreferredDate,
                TimeSlotStart = appointment.TimeSlotStart,
                TimeSlotEnd = appointment.TimeSlotEnd,
                Status = appointment.Status,
                AssignmentType = appointment.AssignmentType,
                WorkshopReferenceNumber = appointment.WorkshopReferenceNumber,
                Notes = appointment.Notes,
                CreatedAt = appointment.CreatedAt
            };
        }

        private static WorkshopRepairEstimateResponse? MapWorkshopRepairEstimate(Motor.Claim.Domain.Entities.WorkshopRepairEstimateEntity? estimate)
        {
            return estimate == null ? null : Motor.Claim.Application.Services.WorkshopRepairEstimateService.MapResponse(estimate);
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
