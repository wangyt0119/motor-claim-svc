using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Motor.Claim.Application.Dtos.Claim;
using Motor.Claim.Application.Features.Claim.Commands;
using Motor.Claim.Application.Features.Claim.Queries;
using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Services;

namespace Motor.Claim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimController : ControllerBase
    {
        private readonly CreateClaimCommandHandler _createClaimCommandHandler;
        private readonly GetMyClaimsQueryHandler _getMyClaimsQueryHandler;
        private readonly GetAllClaimsQueryHandler _getAllClaimsQueryHandler;
        private readonly ClaimService _claimService;

        public ClaimController(
            CreateClaimCommandHandler createClaimCommandHandler,
            GetMyClaimsQueryHandler getMyClaimsQueryHandler,
            GetAllClaimsQueryHandler getAllClaimsQueryHandler,
            ClaimService claimService)
        {
            _createClaimCommandHandler = createClaimCommandHandler;
            _getMyClaimsQueryHandler = getMyClaimsQueryHandler;
            _getAllClaimsQueryHandler = getAllClaimsQueryHandler;
            _claimService = claimService;
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

            var response = result.Select(MapResponse).ToList();

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

        [HttpPost("{id:guid}/approve")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] OfficerDecisionRequest request)
        {
            try
            {
                var officerUserId = GetCurrentUserId();
                var result = await _claimService.ApproveAsync(id, officerUserId, request.Note);
                return Ok(MapResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] OfficerDecisionRequest request)
        {
            try
            {
                var officerUserId = GetCurrentUserId();
                var result = await _claimService.RejectAsync(id, officerUserId, request.Note);
                return Ok(MapResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:guid}/request-info")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> RequestInfo(Guid id, [FromBody] RequestInfoRequest request)
        {
            try
            {
                var officerUserId = GetCurrentUserId();
                var result = await _claimService.RequestInfoAsync(id, officerUserId, request.RequestedItems, request.Note);
                return Ok(MapResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:guid}/customer-response")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> SubmitCustomerResponse(Guid id, [FromBody] CustomerResponseRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _claimService.SubmitCustomerResponseAsync(id, userId, request.ResponseNote, request.ResponseDocuments);
                return Ok(MapResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
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
                ReviewedByUserId = claim.ReviewedByUserId,
                WorkshopAppointment = MapWorkshopAppointment(claim.WorkshopAppointment),
                WorkshopRepairEstimate = MapWorkshopRepairEstimate(claim.WorkshopRepairEstimate),
                WorkshopPayment = claim.WorkshopPayment == null ? null : WorkshopPaymentService.MapResponse(claim.WorkshopPayment)
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
                Notes = appointment.Notes,
                CreatedAt = appointment.CreatedAt
            };
        }

        private static WorkshopRepairEstimateResponse? MapWorkshopRepairEstimate(Motor.Claim.Domain.Entities.WorkshopRepairEstimateEntity? estimate)
        {
            return estimate == null ? null : WorkshopRepairEstimateService.MapResponse(estimate);
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
