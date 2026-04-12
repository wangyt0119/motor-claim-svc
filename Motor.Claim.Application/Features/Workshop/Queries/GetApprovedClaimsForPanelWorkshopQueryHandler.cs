using System.Text.Json;
using Motor.Claim.Application.Dtos.Claim;
using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Application.Services;

namespace Motor.Claim.Application.Features.Workshop.Queries
{
    public class GetApprovedClaimsForPanelWorkshopQueryHandler
    {
        private readonly IClaimRepository _claimRepository;

        public GetApprovedClaimsForPanelWorkshopQueryHandler(IClaimRepository claimRepository)
        {
            _claimRepository = claimRepository;
        }

        public async Task<List<ClaimResponse>> Handle(GetApprovedClaimsForPanelWorkshopQuery query)
        {
            var claims = await _claimRepository.GetApprovedClaimsByWorkshopIdAsync(query.WorkshopId);
            return claims.Select(MapClaimResponse).ToList();
        }

        private static ClaimResponse MapClaimResponse(Motor.Claim.Domain.Entities.ClaimEntity claim)
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
                ValidationResult = claim.ValidationResult,
                OfficerDecisionNote = claim.OfficerDecisionNote,
                RequestedItems = claim.RequestedItems,
                CustomerResponseNote = claim.CustomerResponseNote,
                ResponseDocuments = DeserializeDocuments(claim.ResponseDocuments),
                RequestedAt = claim.RequestedAt,
                RespondedAt = claim.RespondedAt,
                DecidedAt = claim.DecidedAt,
                ReviewedByUserId = claim.ReviewedByUserId,
                WorkshopAppointment = MapWorkshopAppointment(claim.WorkshopAppointment),
                WorkshopRepairEstimate = claim.WorkshopRepairEstimate == null
                    ? null
                    : WorkshopRepairEstimateService.MapResponse(claim.WorkshopRepairEstimate)
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
    }
}
