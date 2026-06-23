using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Services
{
    public class WorkshopClaimLinkRequestService
    {
        private readonly IWorkshopClaimLinkRequestRepository _linkRequestRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IWorkshopRepository _workshopRepository;

        public WorkshopClaimLinkRequestService(
            IWorkshopClaimLinkRequestRepository linkRequestRepository,
            IClaimRepository claimRepository,
            IWorkshopRepository workshopRepository)
        {
            _linkRequestRepository = linkRequestRepository;
            _claimRepository = claimRepository;
            _workshopRepository = workshopRepository;
        }

        public async Task<WorkshopClaimLinkRequestResponse> CreateAsync(
            Guid workshopId,
            CreateWorkshopClaimLinkRequest request)
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(request.ClaimId)
                ?? throw new ArgumentException("Claim not found.");
            var workshop = await GetActivePanelWorkshopAsync(workshopId);

            ValidateEligibleClaim(claim);
            ValidateArrivalDate(claim, request.ArrivalDate);

            if (await _linkRequestRepository.GetPendingByClaimIdAsync(request.ClaimId) != null)
            {
                throw new ArgumentException("This claim already has a pending workshop link request.");
            }

            var linkRequest = new WorkshopClaimLinkRequestEntity
            {
                RequestId = Guid.NewGuid(),
                ClaimId = request.ClaimId,
                WorkshopId = workshopId,
                ArrivalDate = request.ArrivalDate.Date,
                Status = "Pending",
                WorkshopReferenceNumber = NormalizeOptional(request.WorkshopReferenceNumber),
                Notes = NormalizeOptional(request.Notes),
                CreatedAt = DateTime.UtcNow,
                Claim = claim,
                Workshop = workshop
            };

            await _linkRequestRepository.AddAsync(linkRequest);
            return MapResponse(linkRequest);
        }

        public async Task<List<WorkshopClaimLinkRequestResponse>> GetForCustomerAsync(Guid customerId)
        {
            var requests = await _linkRequestRepository.GetByCustomerIdAsync(customerId);
            return requests.Select(MapResponse).ToList();
        }

        public async Task<List<WorkshopClaimLinkRequestResponse>> GetForWorkshopAsync(Guid workshopId)
        {
            await GetActivePanelWorkshopAsync(workshopId);
            var requests = await _linkRequestRepository.GetByWorkshopIdAsync(workshopId);
            return requests.Select(MapResponse).ToList();
        }

        public async Task<WorkshopAppointmentResponse> AcceptAsync(Guid customerId, Guid requestId)
        {
            var request = await GetPendingCustomerRequestAsync(customerId, requestId);
            ValidateEligibleClaim(request.Claim);
            await GetActivePanelWorkshopAsync(request.WorkshopId);

            var appointment = new WorkshopAppointmentEntity
            {
                AppointmentId = Guid.NewGuid(),
                ClaimId = request.ClaimId,
                WorkshopId = request.WorkshopId,
                PreferredDate = request.ArrivalDate.Date,
                TimeSlotStart = TimeSpan.Zero,
                TimeSlotEnd = TimeSpan.Zero,
                Status = "VehicleAtWorkshop",
                AssignmentType = "WorkshopRequestedLink",
                WorkshopReferenceNumber = request.WorkshopReferenceNumber,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                Workshop = request.Workshop
            };

            await _linkRequestRepository.AcceptAsync(request, appointment);
            return MapAppointmentResponse(appointment);
        }

        public async Task<WorkshopClaimLinkRequestResponse> RejectAsync(
            Guid customerId,
            Guid requestId,
            RejectWorkshopClaimLinkRequest request)
        {
            var linkRequest = await GetPendingCustomerRequestAsync(customerId, requestId);
            linkRequest.Status = "Rejected";
            linkRequest.CustomerResponseNote = NormalizeOptional(request.Reason);
            linkRequest.RespondedAt = DateTime.UtcNow;
            await _linkRequestRepository.UpdateAsync(linkRequest);
            return MapResponse(linkRequest);
        }

        private async Task<WorkshopClaimLinkRequestEntity> GetPendingCustomerRequestAsync(
            Guid customerId,
            Guid requestId)
        {
            var request = await _linkRequestRepository.GetByIdWithDetailsAsync(requestId)
                ?? throw new ArgumentException("Workshop link request not found.");

            if (request.Claim.UserId != customerId)
            {
                throw new ArgumentException("You are not allowed to respond to this workshop link request.");
            }

            if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("This workshop link request has already been processed.");
            }

            return request;
        }

        private async Task<WorkshopEntity> GetActivePanelWorkshopAsync(Guid workshopId)
        {
            var workshop = await _workshopRepository.GetByIdAsync(workshopId);
            if (workshop == null || !workshop.IsActive || !workshop.IsPanelWorkshop)
            {
                throw new ArgumentException("Workshop is not an active panel workshop.");
            }

            return workshop;
        }

        private static void ValidateEligibleClaim(ClaimEntity claim)
        {
            if (string.Equals(claim.Status, "Withdrawn", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("A withdrawn claim cannot be linked to a workshop.");
            }

            if (claim.AllClaimType != AllClaimType.VehicleClaim)
            {
                throw new ArgumentException("Workshop linking is only available for vehicle claims.");
            }

            var isOfficerApproved = string.Equals(claim.ReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase);
            var isStpApproved = claim.STPStatus == StpStatus.AutoApproved || claim.IsSTPApproved;
            if (!isOfficerApproved && !isStpApproved)
            {
                throw new ArgumentException("Workshop linking is only available after STP approval or officer approval.");
            }

            if (claim.WorkshopRepairEstimate != null)
            {
                throw new ArgumentException("The workshop cannot be changed after a quotation has been submitted.");
            }

            if (claim.WorkshopAppointment != null)
            {
                throw new ArgumentException("This claim is already assigned to a workshop.");
            }
        }

        private static void ValidateArrivalDate(ClaimEntity claim, DateTime arrivalDate)
        {
            if (arrivalDate == default)
            {
                throw new ArgumentException("Vehicle arrival date is required.");
            }

            if (arrivalDate.Date < claim.IncidentDate.Date)
            {
                throw new ArgumentException("Vehicle arrival date cannot be earlier than the incident date.");
            }

            if (arrivalDate.Date > DateTime.Today)
            {
                throw new ArgumentException("Vehicle arrival date cannot be in the future.");
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static WorkshopClaimLinkRequestResponse MapResponse(WorkshopClaimLinkRequestEntity request)
        {
            return new WorkshopClaimLinkRequestResponse
            {
                RequestId = request.RequestId,
                ClaimId = request.ClaimId,
                WorkshopId = request.WorkshopId,
                WorkshopName = request.Workshop.Name,
                WorkshopState = request.Workshop.State,
                ArrivalDate = request.ArrivalDate,
                Status = request.Status,
                WorkshopReferenceNumber = request.WorkshopReferenceNumber,
                Notes = request.Notes,
                CustomerResponseNote = request.CustomerResponseNote,
                CreatedAt = request.CreatedAt,
                RespondedAt = request.RespondedAt
            };
        }

        private static WorkshopAppointmentResponse MapAppointmentResponse(WorkshopAppointmentEntity appointment)
        {
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
    }
}
