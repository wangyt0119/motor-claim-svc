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
        private readonly IClaimRepository _claimRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailNotificationService _emailNotificationService;
        
        public WorkshopService(
            IWorkshopRepository workshopRepository,
            IWorkshopAppointmentRepository workshopAppointmentRepository,
            IClaimRepository claimRepository,
            IUserRepository userRepository,
            IEmailNotificationService emailNotificationService)
        {
            _workshopRepository = workshopRepository;
            _workshopAppointmentRepository = workshopAppointmentRepository;
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

            if (claim.AllClaimType != AllClaimType.VehicleClaim)
            {
                throw new ArgumentException("Workshop booking is only available for vehicle claims.");
            }

            var isOfficerApproved = string.Equals(claim.ReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase);
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

            var workshop = await _workshopRepository.GetByIdAsync(request.WorkshopId);
            if (workshop == null || !workshop.IsActive || !workshop.IsPanelWorkshop)
            {
                throw new ArgumentException("Selected workshop is not an active panel workshop.");
            }

            var existingAppointment = await _workshopAppointmentRepository.GetByClaimIdAsync(request.ClaimId);

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

                await _workshopAppointmentRepository.UpdateAsync(existingAppointment);
            }

            var savedAppointment = await _workshopAppointmentRepository.GetByClaimIdAsync(request.ClaimId)
                ?? throw new InvalidOperationException("Workshop appointment could not be loaded after save.");

            await SendWorkshopAppointmentNotificationsAsync(claim, workshop, savedAppointment);
            return MapAppointmentResponse(savedAppointment);
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
                Notes = appointment.Notes,
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
            var customer = await _userRepository.GetByIdAsync(claim.UserId);
            if (customer != null && !string.IsNullOrWhiteSpace(customer.Email))
            {
                await _emailNotificationService.SendAsync(
                    customer.Email,
                    "Your panel workshop has been selected",
                    WrapEmail(
                        customer.FullName,
                        BuildWorkshopAppointmentEmailBody(claim, workshop, appointment, false)));
            }

            foreach (var workshopEmail in DeserializeOptionalList(workshop.Email).Where(IsValidEmail))
            {
                await _emailNotificationService.SendAsync(
                    workshopEmail,
                    "A new claim has selected your workshop",
                    WrapEmail(
                        workshop.Name,
                        BuildWorkshopAppointmentEmailBody(claim, workshop, appointment, true)));
            }
        }

        private static string BuildWorkshopAppointmentEmailBody(
            ClaimEntity claim,
            WorkshopEntity workshop,
            WorkshopAppointmentEntity appointment,
            bool forWorkshop)
        {
            var builder = new StringBuilder();

            if (forWorkshop)
            {
                builder.AppendLine("<p>A customer has selected your panel workshop for an approved motor claim.</p>");
            }
            else
            {
                builder.AppendLine("<p>Your panel workshop selection has been recorded successfully.</p>");
            }

            builder.AppendLine($"<p><strong>Claim ID:</strong> {claim.ClaimId}</p>");
            builder.AppendLine($"<p><strong>Workshop:</strong> {WebUtility.HtmlEncode(workshop.Name)}</p>");
            builder.AppendLine($"<p><strong>State:</strong> {WebUtility.HtmlEncode(workshop.State)}</p>");
            builder.AppendLine($"<p><strong>Address:</strong> {WebUtility.HtmlEncode(workshop.Address)}</p>");
            builder.AppendLine($"<p><strong>Preferred Date:</strong> {appointment.PreferredDate:dd MMM yyyy}</p>");
            builder.AppendLine($"<p><strong>Time Slot:</strong> {appointment.TimeSlotStart:hh\\:mm} - {appointment.TimeSlotEnd:hh\\:mm}</p>");

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
