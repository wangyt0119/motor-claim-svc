using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Features.Workshop.Commands;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Motor.Claim.Application.Services
{
    public class WorkshopService
    {
        private readonly IWorkshopRepository _workshopRepository;
        private readonly IWorkshopAppointmentRepository _workshopAppointmentRepository;
        private readonly IWorkshopRepairEstimateRepository _workshopRepairEstimateRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailNotificationService _emailNotificationService;
        
        public WorkshopService(
            IWorkshopRepository workshopRepository,
            IWorkshopAppointmentRepository workshopAppointmentRepository,
            IWorkshopRepairEstimateRepository workshopRepairEstimateRepository,
            IClaimRepository claimRepository,
            IUserRepository userRepository,
            IEmailNotificationService emailNotificationService)
        {
            _workshopRepository = workshopRepository;
            _workshopAppointmentRepository = workshopAppointmentRepository;
            _workshopRepairEstimateRepository = workshopRepairEstimateRepository;
            _claimRepository = claimRepository;
            _userRepository = userRepository;
            _emailNotificationService = emailNotificationService;
        }

        public async Task<List<string>> GetPanelStatesAsync()
        {
            return await _workshopRepository.GetActivePanelStatesAsync();
        }

        public async Task<List<WorkshopResponse>> GetAllWorkshopsAsync()
        {
            var workshops = await _workshopRepository.GetAllAsync();
            return workshops.OrderBy(x => x.State).ThenBy(x => x.Name).Select(MapWorkshopResponse).ToList();
        }

        public async Task<List<WorkshopResponse>> GetPanelWorkshopsByStateAsync(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                throw new ArgumentException("State is required.");
            }

            var workshops = await _workshopRepository.GetActivePanelWorkshopsByStateAsync(state);

            return workshops.Select(MapWorkshopResponse).ToList();
        }

        public async Task<WorkshopEntity> CreateWorkshopAsync(CreateWorkshopCommand command)
        {
            ValidateWorkshopFields(command.Name, command.State, command.Address);

            var workshop = new WorkshopEntity
            {
                WorkshopId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Name = command.Name.Trim(),
                State = command.State.Trim(),
                Address = command.Address.Trim(),
                Phone = SerializeOptionalList(command.Phone),
                Fax = NormalizeOptional(command.Fax),
                Email = SerializeOptionalList(command.Email),
                BankName = NormalizeOptional(command.BankName),
                BankAccountNumber = NormalizeOptional(command.BankAccountNumber),
                BankAccountHolderName = NormalizeOptional(command.BankAccountHolderName),
                IsPanelWorkshop = command.IsPanelWorkshop,
                IsActive = command.IsActive
            };

            return await _workshopRepository.AddAsync(workshop);
        }

        public async Task<WorkshopEntity> UpdateWorkshopAsync(UpdateWorkshopCommand command)
        {
            ValidateWorkshopFields(command.Name, command.State, command.Address);

            var workshop = await _workshopRepository.GetByIdAsync(command.WorkshopId);
            if (workshop == null)
            {
                throw new ArgumentException("Workshop not found.");
            }

            workshop.Name = command.Name.Trim();
            workshop.State = command.State.Trim();
            workshop.Address = command.Address.Trim();
            workshop.Phone = SerializeOptionalList(command.Phone);
            workshop.Fax = NormalizeOptional(command.Fax);
            workshop.Email = SerializeOptionalList(command.Email);
            workshop.BankName = NormalizeOptional(command.BankName);
            workshop.BankAccountNumber = NormalizeOptional(command.BankAccountNumber);
            workshop.BankAccountHolderName = NormalizeOptional(command.BankAccountHolderName);
            workshop.IsPanelWorkshop = command.IsPanelWorkshop;
            workshop.IsActive = command.IsActive;

            await _workshopRepository.UpdateAsync(workshop);
            return workshop;
        }

        public async Task DeleteWorkshopAsync(Guid workshopId)
        {
            var workshop = await _workshopRepository.GetByIdAsync(workshopId);
            if (workshop == null)
            {
                throw new ArgumentException("Workshop not found.");
            }

            await _workshopRepository.DeleteAsync(workshopId);
        }

        public async Task<WorkshopAppointmentResponse> CreateOrUpdateAppointmentAsync(Guid userId, CreateWorkshopAppointmentRequest request)
        {
            var claim = await _claimRepository.GetByIdAsync(request.ClaimId);
            if (claim == null)
            {
                throw new ArgumentException("Claim not found.");
            }

            if (claim.UserId != userId)
            {
                throw new ArgumentException("You are not allowed to book a workshop for this claim.");
            }

            if (string.Equals(claim.Status, "Withdrawn", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("A withdrawn claim cannot be booked with a workshop.");
            }

            if (claim.AllClaimType != AllClaimType.VehicleClaim)
            {
                throw new ArgumentException("Workshop booking is only available for vehicle claims.");
            }

            var isOfficerApproved =
                string.Equals(claim.ReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Status, "Approved", StringComparison.OrdinalIgnoreCase);
            var isStpApproved = claim.STPStatus == StpStatus.AutoApproved || claim.IsSTPApproved;

            if (!isOfficerApproved && !isStpApproved)
            {
                throw new ArgumentException("Workshop booking is only available after STP approval or officer approval.");
            }

            if (request.PreferredDate.Date < DateTime.Today)
            {
                throw new ArgumentException("Preferred workshop date cannot be in the past.");
            }

            if (request.TimeSlotEnd <= request.TimeSlotStart)
            {
                throw new ArgumentException("Time slot end must be later than time slot start.");
            }

            await EnsureNoWorkshopQuotationAsync(request.ClaimId);

            var workshop = await _workshopRepository.GetByIdAsync(request.WorkshopId);
            if (workshop == null || !workshop.IsActive || !workshop.IsPanelWorkshop)
            {
                throw new ArgumentException("Selected workshop is not an active panel workshop.");
            }

            var existingAppointment = await _workshopAppointmentRepository.GetByClaimIdAsync(request.ClaimId);
            var conflictingAppointment = await _workshopAppointmentRepository.GetConflictingScheduledSlotAsync(
                request.WorkshopId,
                request.PreferredDate,
                request.TimeSlotStart,
                request.TimeSlotEnd,
                request.ClaimId);

            if (conflictingAppointment != null)
            {
                throw new ArgumentException("This workshop time slot is already booked. Please choose another date or time.");
            }

            if (existingAppointment == null)
            {
                existingAppointment = new WorkshopAppointmentEntity
                {
                    AppointmentId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    ClaimId = request.ClaimId,
                    WorkshopId = request.WorkshopId,
                    PreferredDate = request.PreferredDate.Date,
                    TimeSlotStart = request.TimeSlotStart,
                    TimeSlotEnd = request.TimeSlotEnd,
                    Status = "Pending",
                    AssignmentType = "ScheduledAppointment",
                    Notes = request.Notes
                };

                await _workshopAppointmentRepository.AddAsync(existingAppointment);
            }
            else
            {
                existingAppointment.WorkshopId = request.WorkshopId;
                existingAppointment.PreferredDate = request.PreferredDate.Date;
                existingAppointment.TimeSlotStart = request.TimeSlotStart;
                existingAppointment.TimeSlotEnd = request.TimeSlotEnd;
                existingAppointment.Notes = request.Notes;
                existingAppointment.Status = "Pending";
                existingAppointment.AssignmentType = "ScheduledAppointment";
                existingAppointment.WorkshopReferenceNumber = null;

                await _workshopAppointmentRepository.UpdateAsync(existingAppointment);
            }

            var savedAppointment = await _workshopAppointmentRepository.GetByClaimIdAsync(request.ClaimId)
                ?? throw new InvalidOperationException("Workshop appointment could not be loaded after save.");

            await SendWorkshopAppointmentNotificationsAsync(claim, workshop, savedAppointment);
            return MapAppointmentResponse(savedAppointment);
        }

        public async Task<WorkshopAppointmentResponse> AssignVehicleAlreadyAtWorkshopAsync(
            Guid userId,
            AssignVehicleAlreadyAtWorkshopRequest request)
        {
            var claim = await _claimRepository.GetByIdAsync(request.ClaimId);
            if (claim == null)
            {
                throw new ArgumentException("Claim not found.");
            }

            if (claim.UserId != userId)
            {
                throw new ArgumentException("You are not allowed to assign a workshop for this claim.");
            }

            if (string.Equals(claim.Status, "Withdrawn", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("A withdrawn claim cannot be assigned to a workshop.");
            }

            if (claim.AllClaimType != AllClaimType.VehicleClaim)
            {
                throw new ArgumentException("Workshop assignment is only available for vehicle claims.");
            }

            var isOfficerApproved =
                string.Equals(claim.ReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Status, "Approved", StringComparison.OrdinalIgnoreCase);
            var isStpApproved = claim.STPStatus == StpStatus.AutoApproved || claim.IsSTPApproved;
            if (!isOfficerApproved && !isStpApproved)
            {
                throw new ArgumentException("Workshop assignment is only available after STP approval or officer approval.");
            }

            if (request.ArrivalDate == default)
            {
                throw new ArgumentException("Vehicle arrival date is required.");
            }

            if (request.ArrivalDate.Date < claim.IncidentDate.Date)
            {
                throw new ArgumentException("Vehicle arrival date cannot be earlier than the incident date.");
            }

            if (request.ArrivalDate.Date > DateTime.Today)
            {
                throw new ArgumentException("Vehicle arrival date cannot be in the future.");
            }

            await EnsureNoWorkshopQuotationAsync(request.ClaimId);

            var workshop = await _workshopRepository.GetByIdAsync(request.WorkshopId);
            if (workshop == null || !workshop.IsActive || !workshop.IsPanelWorkshop)
            {
                throw new ArgumentException("Selected workshop is not an active panel workshop.");
            }

            var existingAssignment = await _workshopAppointmentRepository.GetByClaimIdAsync(request.ClaimId);
            var isNewAssignment = existingAssignment == null;
            if (existingAssignment == null)
            {
                existingAssignment = new WorkshopAppointmentEntity
                {
                    AppointmentId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    ClaimId = request.ClaimId
                };
            }

            existingAssignment.WorkshopId = request.WorkshopId;
            existingAssignment.PreferredDate = request.ArrivalDate.Date;
            existingAssignment.TimeSlotStart = TimeSpan.Zero;
            existingAssignment.TimeSlotEnd = TimeSpan.Zero;
            existingAssignment.Status = "VehicleAtWorkshop";
            existingAssignment.AssignmentType = "AlreadyAtWorkshop";
            existingAssignment.WorkshopReferenceNumber = NormalizeOptional(request.WorkshopReferenceNumber);
            existingAssignment.Notes = NormalizeOptional(request.Notes);

            if (isNewAssignment)
            {
                await _workshopAppointmentRepository.AddAsync(existingAssignment);
            }
            else
            {
                await _workshopAppointmentRepository.UpdateAsync(existingAssignment);
            }

            var savedAssignment = await _workshopAppointmentRepository.GetByClaimIdAsync(request.ClaimId)
                ?? throw new InvalidOperationException("Workshop assignment could not be loaded after save.");

            await SendWorkshopAppointmentNotificationsAsync(claim, workshop, savedAssignment);
            return MapAppointmentResponse(savedAssignment);
        }

        public async Task<WorkshopAppointmentResponse?> GetAppointmentByClaimAsync(Guid userId, Guid claimId, bool enforceOwnership = true)
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                throw new ArgumentException("Claim not found.");
            }

            if (enforceOwnership && claim.UserId != userId)
            {
                throw new ArgumentException("You are not allowed to view this workshop appointment.");
            }

            var appointment = await _workshopAppointmentRepository.GetByClaimIdAsync(claimId);
            return appointment == null ? null : MapAppointmentResponse(appointment);
        }

        public async Task<List<WorkshopBookedSlotResponse>> GetBookedSlotsAsync(
            Guid workshopId,
            DateTime preferredDate,
            Guid? excludedClaimId = null)
        {
            var workshop = await _workshopRepository.GetByIdAsync(workshopId);
            if (workshop == null || !workshop.IsActive || !workshop.IsPanelWorkshop)
            {
                throw new ArgumentException("Selected workshop is not an active panel workshop.");
            }

            var slots = await _workshopAppointmentRepository.GetScheduledSlotsAsync(
                workshopId,
                preferredDate,
                excludedClaimId);

            return slots
                .Select(slot => new WorkshopBookedSlotResponse
                {
                    TimeSlotStart = slot.TimeSlotStart,
                    TimeSlotEnd = slot.TimeSlotEnd
                })
                .ToList();
        }

        public async Task<WorkshopResponse> GetMyWorkshopAsync(Guid workshopId)
        {
            var workshop = await _workshopRepository.GetByIdAsync(workshopId);
            if (workshop == null)
            {
                throw new ArgumentException("Workshop not found.");
            }

            return MapWorkshopResponse(workshop);
        }

        public async Task<WorkshopResponse> UpdateMyWorkshopAsync(Guid workshopId, UpdateMyWorkshopRequest request)
        {
            ValidateWorkshopFields(request.Name, request.State, request.Address);

            var workshop = await _workshopRepository.GetByIdAsync(workshopId);
            if (workshop == null)
            {
                throw new ArgumentException("Workshop not found.");
            }

            workshop.Name = request.Name.Trim();
            workshop.State = request.State.Trim();
            workshop.Address = request.Address.Trim();
            workshop.Phone = SerializeOptionalList(request.Phone);
            workshop.Fax = NormalizeOptional(request.Fax);
            workshop.Email = SerializeOptionalList(request.Email);
            workshop.BankName = NormalizeOptional(request.BankName);
            workshop.BankAccountNumber = NormalizeOptional(request.BankAccountNumber);
            workshop.BankAccountHolderName = NormalizeOptional(request.BankAccountHolderName);

            await _workshopRepository.UpdateAsync(workshop);
            return MapWorkshopResponse(workshop);
        }

        private static WorkshopResponse MapWorkshopResponse(WorkshopEntity workshop)
        {
            return new WorkshopResponse
            {
                WorkshopId = workshop.WorkshopId,
                Name = workshop.Name,
                State = workshop.State,
                Address = workshop.Address,
                Phone = DeserializeOptionalList(workshop.Phone),
                Fax = workshop.Fax,
                Email = DeserializeOptionalList(workshop.Email),
                BankName = workshop.BankName,
                BankAccountNumber = workshop.BankAccountNumber,
                BankAccountHolderName = workshop.BankAccountHolderName,
                StripeConnectedAccountId = workshop.StripeConnectedAccountId,
                StripeOnboardingStatus = workshop.StripeOnboardingStatus,
                StripeChargesEnabled = workshop.StripeChargesEnabled,
                StripePayoutsEnabled = workshop.StripePayoutsEnabled,
                StripeLastSyncedAt = workshop.StripeLastSyncedAt,
                IsPanelWorkshop = workshop.IsPanelWorkshop,
                IsActive = workshop.IsActive
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
                EmailNotificationSent = appointment.EmailNotificationSent,
                EmailNotificationMessage = appointment.EmailNotificationMessage,
                CreatedAt = appointment.CreatedAt
            };
        }

        private static void ValidateWorkshopFields(string name, string state, string address)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Workshop name is required.");
            }

            if (string.IsNullOrWhiteSpace(state))
            {
                throw new ArgumentException("Workshop state is required.");
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Workshop address is required.");
            }
        }

        private async Task EnsureNoWorkshopQuotationAsync(Guid claimId)
        {
            if (await _workshopRepairEstimateRepository.GetByClaimIdAsync(claimId) != null)
            {
                throw new ArgumentException("The assigned workshop cannot be changed after a quotation has been submitted.");
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? SerializeOptionalList(List<string>? values)
        {
            var normalized = values?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToList() ?? new List<string>();

            return normalized.Count == 0 ? null : JsonSerializer.Serialize(normalized);
        }

        private static List<string> DeserializeOptionalList(string? payload)
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
                return new List<string> { payload.Trim() };
            }
        }

        private async Task SendWorkshopAppointmentNotificationsAsync(
            ClaimEntity claim,
            WorkshopEntity workshop,
            WorkshopAppointmentEntity appointment)
        {
            var notificationMessages = new List<string>();
            var allSucceeded = true;

            var customer = await _userRepository.GetByIdAsync(claim.UserId);
            if (customer != null && !string.IsNullOrWhiteSpace(customer.Email))
            {
                var customerResult = await _emailNotificationService.SendDiagnosticAsync(
                    customer.Email,
                    "Your panel workshop has been selected",
                    WrapEmail(
                        customer.FullName,
                        BuildWorkshopAppointmentEmailBody(claim, workshop, appointment, false)));

                allSucceeded &= customerResult.Success;
                notificationMessages.Add($"Customer email: {customerResult.Message}");
            }
            else
            {
                allSucceeded = false;
                notificationMessages.Add("Customer email: Customer email address was not found.");
            }

            var workshopEmails = DeserializeOptionalList(workshop.Email).Where(IsValidEmail).ToList();
            if (workshopEmails.Count == 0)
            {
                allSucceeded = false;
                notificationMessages.Add("Workshop email: Workshop email address was not found.");
            }

            foreach (var workshopEmail in workshopEmails)
            {
                var workshopResult = await _emailNotificationService.SendDiagnosticAsync(
                    workshopEmail,
                    "A new claim has selected your workshop",
                    WrapEmail(
                        workshop.Name,
                        BuildWorkshopAppointmentEmailBody(claim, workshop, appointment, true)));

                allSucceeded &= workshopResult.Success;
                notificationMessages.Add($"Workshop email ({workshopEmail}): {workshopResult.Message}");
            }

            appointment.EmailNotificationSent = allSucceeded;
            appointment.EmailNotificationMessage = string.Join(" | ", notificationMessages);
        }

        private static string BuildWorkshopAppointmentEmailBody(
            ClaimEntity claim,
            WorkshopEntity workshop,
            WorkshopAppointmentEntity appointment,
            bool forWorkshop)
        {
            var builder = new StringBuilder();
            var isAlreadyAtWorkshop = string.Equals(
                appointment.AssignmentType,
                "AlreadyAtWorkshop",
                StringComparison.OrdinalIgnoreCase);

            if (forWorkshop)
            {
                builder.AppendLine(isAlreadyAtWorkshop
                    ? "<p>A customer has confirmed that their vehicle is already at your panel workshop for an approved motor claim.</p>"
                    : "<p>A customer has selected your panel workshop for an approved motor claim.</p>");
            }
            else
            {
                builder.AppendLine(isAlreadyAtWorkshop
                    ? "<p>Your vehicle-at-workshop assignment has been recorded successfully.</p>"
                    : "<p>Your panel workshop selection has been recorded successfully.</p>");
            }

            builder.AppendLine($"<p><strong>Claim ID:</strong> {claim.ClaimId}</p>");
            builder.AppendLine($"<p><strong>Workshop:</strong> {WebUtility.HtmlEncode(workshop.Name)}</p>");
            builder.AppendLine($"<p><strong>State:</strong> {WebUtility.HtmlEncode(workshop.State)}</p>");
            builder.AppendLine($"<p><strong>Address:</strong> {WebUtility.HtmlEncode(workshop.Address)}</p>");
            if (isAlreadyAtWorkshop)
            {
                builder.AppendLine($"<p><strong>Vehicle Arrival Date:</strong> {appointment.PreferredDate:dd MMM yyyy}</p>");
            }
            else
            {
                builder.AppendLine($"<p><strong>Preferred Date:</strong> {appointment.PreferredDate:dd MMM yyyy}</p>");
                builder.AppendLine($"<p><strong>Time Slot:</strong> {appointment.TimeSlotStart:hh\\:mm} - {appointment.TimeSlotEnd:hh\\:mm}</p>");
            }

            if (!string.IsNullOrWhiteSpace(appointment.WorkshopReferenceNumber))
            {
                builder.AppendLine($"<p><strong>Workshop Reference:</strong> {WebUtility.HtmlEncode(appointment.WorkshopReferenceNumber)}</p>");
            }

            if (!string.IsNullOrWhiteSpace(appointment.Notes))
            {
                builder.AppendLine($"<p><strong>Notes:</strong> {WebUtility.HtmlEncode(appointment.Notes)}</p>");
            }

            return builder.ToString();
        }

        private static string WrapEmail(string recipientName, string content)
        {
            return $"""
                <div style="font-family: Arial, sans-serif; color: #1f2937; line-height: 1.6;">
                    <p>Hello {WebUtility.HtmlEncode(recipientName)},</p>
                    {content}
                    <p>Regards,<br />Motor Claim System</p>
                </div>
                """;
        }

        private static bool IsValidEmail(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Contains('@');
        }
    }
}
