using System.Net;
using System.Text;
using System.Text.Json;
using Motor.Claim.Application.Dtos.Stp;
using Motor.Claim.Application.Features.Claim.Commands;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Services
{
    public class ClaimService
    {
        private const int RepeatClaimManualReviewWindowDays = 30;
        private static readonly HashSet<string> WithdrawableStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Pending",
            "Pending Manual Review",
            "Pending Customer Action",
            "Customer Responded",
            "Approved"
        };

        private readonly IClaimRepository _claimRepository;
        private readonly ICoverageRepository _coverageRepository;
        private readonly IWorkshopAppointmentRepository _workshopAppointmentRepository;
        private readonly StpValidationService _stpValidationService;
        private readonly IUserRepository _userRepository;
        private readonly IEmailNotificationService _emailNotificationService;

        public ClaimService(
            IClaimRepository claimRepository,
            ICoverageRepository coverageRepository,
            IWorkshopAppointmentRepository workshopAppointmentRepository,
            StpValidationService stpValidationService,
            IUserRepository userRepository,
            IEmailNotificationService emailNotificationService)
        {
            _claimRepository = claimRepository;
            _coverageRepository = coverageRepository;
            _workshopAppointmentRepository = workshopAppointmentRepository;
            _stpValidationService = stpValidationService;
            _userRepository = userRepository;
            _emailNotificationService = emailNotificationService;
        }

        public async Task<ClaimEntity> CreateAsync(CreateClaimCommand command)
        {
            var savedClaim = await CreatePendingAsync(command);
            return await ProcessStpValidationAsync(savedClaim.ClaimId);
        }

        public async Task<ClaimEntity> CreatePendingAsync(CreateClaimCommand command)
        {
            var coverage = await _coverageRepository.GetByIdAsync(command.CoverageId);

            if (coverage == null)
            {
                throw new ArgumentException("Coverage not found.");
            }

            if (coverage.UserId != command.UserId)
            {
                throw new ArgumentException("You are not allowed to claim this coverage.");
            }

            var incidentDateUtc = EnsureUtc(command.IncidentDate);

            if (incidentDateUtc.Date < coverage.EffectiveDate.Date || incidentDateUtc.Date > coverage.ExpiryDate.Date)
            {
                throw new ArgumentException("Incident date must be between the coverage effective date and expiry date.");
            }

            ValidateClaimTypeCoverage(command, coverage);

            var hasActiveClaimForCoverage = await _claimRepository.HasActiveClaimForCoverageAsync(command.CoverageId);
            if (hasActiveClaimForCoverage)
            {
                throw new ArgumentException("This coverage already has an active claim. Wait until it is approved or rejected before submitting another claim.");
            }

            ValidateDocuments(command);

            var manualReviewWindowStart = DateTime.UtcNow.AddDays(-RepeatClaimManualReviewWindowDays);
            var hasRecentClaimForCoverage = await _claimRepository.HasSubmittedClaimForCoverageSinceAsync(
                command.CoverageId,
                manualReviewWindowStart);

            var claim = new ClaimEntity
            {
                CreatedAt = DateTime.UtcNow,
                ClaimId = Guid.NewGuid(),
                UserId = command.UserId,
                CoverageId = command.CoverageId,
                IncidentDate = incidentDateUtc,
                AllClaimType = command.AllClaimType,
                MotorClaimType = command.MotorClaimType,
                IncidentDescription = command.IncidentDescription,
                PoliceReportDocument = command.PoliceReportDocument,
                VehicleOwnershipCertificateDocument = command.VehicleOwnershipCertificateDocument,
                IdentityDocumentFront = command.IdentityDocumentFront,
                IdentityDocumentBack = command.IdentityDocumentBack,
                DrivingLicenseFront = command.DrivingLicenseFront,
                DrivingLicenseBack = command.DrivingLicenseBack,
                VehicleDamageFrontLeftDocument = command.VehicleDamageFrontLeftDocument,
                VehicleDamageFrontRightDocument = command.VehicleDamageFrontRightDocument,
                VehicleDamageRearLeftDocument = command.VehicleDamageRearLeftDocument,
                VehicleDamageRearRightDocument = command.VehicleDamageRearRightDocument,
                IsFlaggedForManualReview = hasRecentClaimForCoverage,
                ManualReviewFlagReason = hasRecentClaimForCoverage
                    ? $"This coverage already has another submitted claim within the last {RepeatClaimManualReviewWindowDays} days."
                    : null,
                Status = "Processing Validation",
                ReviewStatus = "ProcessingStpValidation",
                STPStatus = StpStatus.Pending,
                IsSTPApproved = false
            };

            return await _claimRepository.AddAsync(claim);
        }

        public async Task<ClaimEntity> ProcessStpValidationAsync(Guid claimId)
        {
            var savedClaim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
            if (savedClaim == null)
            {
                throw new ArgumentException("Claim not found.");
            }

            if (string.Equals(savedClaim.Status, "Withdrawn", StringComparison.OrdinalIgnoreCase))
            {
                return savedClaim;
            }

            var coverage = savedClaim.Coverage ?? await _coverageRepository.GetByIdAsync(savedClaim.CoverageId);
            if (coverage == null)
            {
                throw new ArgumentException("Coverage not found.");
            }

            StpValidationResultDto stpResult;
            try
            {
                stpResult = await _stpValidationService.ValidateAsync(savedClaim, coverage);
            }
            catch (Exception ex)
            {
                savedClaim.STPStatus = StpStatus.ManualReview;
                savedClaim.IsSTPApproved = false;
                savedClaim.Status = "Pending Manual Review";
                savedClaim.ReviewStatus = "PendingManualReview";
                savedClaim.DecidedAt = null;
                savedClaim.ManualReviewFlagReason = string.IsNullOrWhiteSpace(savedClaim.ManualReviewFlagReason)
                    ? $"STP validation could not be completed automatically: {ex.Message}"
                    : $"{savedClaim.ManualReviewFlagReason} STP validation could not be completed automatically: {ex.Message}";

                await _claimRepository.UpdateStpValidationResultAsync(savedClaim);
                await SendClaimCreatedNotificationAsync(savedClaim, coverage.VehicleNo);
                return savedClaim;
            }

            savedClaim.STPStatus = stpResult.STPStatus;
            savedClaim.IsSTPApproved = stpResult.IsApproved;
            savedClaim.ValidationResult = StpValidationService.SerializeResult(stpResult);

            if (savedClaim.IsFlaggedForManualReview)
            {
                savedClaim.STPStatus = StpStatus.ManualReview;
                savedClaim.IsSTPApproved = false;
                savedClaim.Status = "Pending Manual Review";
                savedClaim.ReviewStatus = "PendingManualReview";
                savedClaim.DecidedAt = null;
            }
            else
            {
                savedClaim.Status = stpResult.IsApproved ? "Approved" : "Pending Manual Review";
                savedClaim.ReviewStatus = stpResult.IsApproved ? "Approved" : "PendingManualReview";
                savedClaim.DecidedAt = stpResult.IsApproved ? DateTime.UtcNow : null;
            }

            await _claimRepository.UpdateStpValidationResultAsync(savedClaim);
            await SendClaimCreatedNotificationAsync(savedClaim, coverage.VehicleNo);

            return savedClaim;
        }

        public async Task<List<ClaimEntity>> GetByUserIdAsync(Guid userId)
        {
            return await _claimRepository.GetByUserIdAsync(userId);
        }

        public async Task<List<ClaimEntity>> GetAllAsync()
        {
            return await _claimRepository.GetAllAsync();
        }

        public async Task<ClaimEntity> ApproveAsync(Guid claimId, Guid officerUserId, string? note)
        {
            var claim = await GetExistingClaimAsync(claimId);
            EnsureClaimIsNotWithdrawn(claim);
            claim.Status = "Approved";
            claim.ReviewStatus = "Approved";
            claim.OfficerDecisionNote = note;
            claim.DecidedAt = DateTime.UtcNow;
            claim.ReviewedByUserId = officerUserId;

            await _claimRepository.UpdateAsync(claim);
            await NotifyCustomerAsync(
                claim,
                "Your motor claim has been approved",
                BuildClaimStatusEmailBody(
                    claim,
                    "Your claim has been approved.",
                    note,
                    "You can proceed with the next steps in the system, including panel workshop selection if needed."));
            return claim;
        }

        public async Task<ClaimEntity> RejectAsync(Guid claimId, Guid officerUserId, string? note)
        {
            var claim = await GetExistingClaimAsync(claimId);
            EnsureClaimIsNotWithdrawn(claim);
            claim.Status = "Rejected";
            claim.ReviewStatus = "Rejected";
            claim.OfficerDecisionNote = note;
            claim.DecidedAt = DateTime.UtcNow;
            claim.ReviewedByUserId = officerUserId;

            await _claimRepository.UpdateAsync(claim);
            await NotifyCustomerAsync(
                claim,
                "Your motor claim has been rejected",
                BuildClaimStatusEmailBody(
                    claim,
                    "Your claim has been rejected.",
                    note,
                    "Please review the note from the claim officer for more details."));
            return claim;
        }

        public async Task<ClaimEntity> RequestInfoAsync(Guid claimId, Guid officerUserId, string requestedItems, string? note)
        {
            if (string.IsNullOrWhiteSpace(requestedItems))
            {
                throw new ArgumentException("RequestedItems is required.");
            }

            var claim = await GetExistingClaimAsync(claimId);
            EnsureClaimIsNotWithdrawn(claim);
            claim.Status = "Pending Customer Action";
            claim.ReviewStatus = "PendingCustomerAction";
            claim.RequestedItems = requestedItems;
            claim.OfficerDecisionNote = note;
            claim.RequestedAt = DateTime.UtcNow;
            claim.DecidedAt = null;
            claim.ReviewedByUserId = officerUserId;

            await _claimRepository.UpdateAsync(claim);
            await NotifyCustomerAsync(
                claim,
                "More information is needed for your claim",
                BuildClaimStatusEmailBody(
                    claim,
                    "Your claim needs manual review and more information before a decision can be made.",
                    note,
                    $"Requested items: {requestedItems}"));
            return claim;
        }

        public async Task<ClaimEntity> SubmitCustomerResponseAsync(Guid claimId, Guid userId, string? responseNote, List<string> responseDocuments)
        {
            var claim = await GetExistingClaimAsync(claimId);
            EnsureClaimIsNotWithdrawn(claim);

            if (claim.UserId != userId)
            {
                throw new ArgumentException("You are not allowed to respond to this claim.");
            }

            claim.Status = "Customer Responded";
            claim.ReviewStatus = "CustomerResponded";
            claim.CustomerResponseNote = responseNote;
            claim.ResponseDocuments = JsonSerializer.Serialize(responseDocuments ?? new List<string>());
            claim.RespondedAt = DateTime.UtcNow;

            await _claimRepository.UpdateAsync(claim);
            await NotifyCustomerAsync(
                claim,
                "We received your claim response",
                BuildClaimStatusEmailBody(
                    claim,
                    "Your additional response and documents have been submitted successfully.",
                    responseNote,
                    "Our team will review the new information and continue processing your claim."));
            return claim;
        }

        public async Task<ClaimEntity> WithdrawAsync(Guid claimId, Guid userId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Withdrawal reason is required.");
            }

            var claim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
            if (claim == null)
            {
                throw new ArgumentException("Claim not found.");
            }

            if (claim.UserId != userId)
            {
                throw new ArgumentException("You are not allowed to withdraw this claim.");
            }

            if (!WithdrawableStatuses.Contains(claim.Status))
            {
                throw new ArgumentException($"A claim with status '{claim.Status}' cannot be withdrawn.");
            }

            if (claim.WorkshopRepairEstimate != null || claim.WorkshopPayment != null)
            {
                throw new ArgumentException("This claim cannot be withdrawn because the workshop has already submitted a quotation.");
            }

            var withdrawnAt = DateTime.UtcNow;
            if (claim.WorkshopAppointment != null)
            {
                claim.WorkshopAppointment.Status = "Cancelled";
                await _workshopAppointmentRepository.UpdateAsync(claim.WorkshopAppointment);
            }

            claim.Status = "Withdrawn";
            claim.ReviewStatus = "Withdrawn";
            claim.IsSTPApproved = false;
            claim.WithdrawnAt = withdrawnAt;
            claim.WithdrawalReason = reason.Trim();
            claim.DecidedAt = withdrawnAt;

            await _claimRepository.UpdateAsync(claim);
            await NotifyCustomerAsync(
                claim,
                "Your motor claim has been withdrawn",
                BuildClaimStatusEmailBody(
                    claim,
                    "Your claim has been withdrawn successfully.",
                    claim.WithdrawalReason,
                    claim.WorkshopAppointment == null
                        ? "No further processing will take place for this claim."
                        : "Your panel workshop booking has also been cancelled."));
            return claim;
        }

        private static void ValidateDocuments(CreateClaimCommand command)
        {
            var missingDocuments = new List<string>();

            AddIfMissing(missingDocuments, command.IdentityDocumentFront, nameof(command.IdentityDocumentFront));

            if (command.AllClaimType != AllClaimType.VehicleClaim)
            {
                AddIfMissing(missingDocuments, command.PoliceReportDocument, nameof(command.PoliceReportDocument));

                if (missingDocuments.Count > 0)
                {
                    throw new ArgumentException($"Missing required document(s): {string.Join(", ", missingDocuments)}");
                }

                return;
            }

            if (!command.MotorClaimType.HasValue)
            {
                throw new ArgumentException("MotorClaimType is required when AllClaimType is VehicleClaim.");
            }

            if (command.MotorClaimType != MotorClaimType.Windscreen)
            {
                AddIfMissing(missingDocuments, command.PoliceReportDocument, nameof(command.PoliceReportDocument));
            }

            if (command.MotorClaimType == MotorClaimType.VehicleDamages ||
                command.MotorClaimType == MotorClaimType.Windscreen)
            {
                AddIfMissing(missingDocuments, command.VehicleOwnershipCertificateDocument, nameof(command.VehicleOwnershipCertificateDocument));
                AddIfMissing(missingDocuments, command.DrivingLicenseFront, nameof(command.DrivingLicenseFront));
            }

            if (command.MotorClaimType == MotorClaimType.VehicleDamages)
            {
                AddIfMissing(missingDocuments, command.VehicleDamageFrontLeftDocument, nameof(command.VehicleDamageFrontLeftDocument));
                AddIfMissing(missingDocuments, command.VehicleDamageFrontRightDocument, nameof(command.VehicleDamageFrontRightDocument));
                AddIfMissing(missingDocuments, command.VehicleDamageRearLeftDocument, nameof(command.VehicleDamageRearLeftDocument));
                AddIfMissing(missingDocuments, command.VehicleDamageRearRightDocument, nameof(command.VehicleDamageRearRightDocument));
            }

            if (command.MotorClaimType == MotorClaimType.Windscreen)
            {
                AddIfMissing(missingDocuments, command.IdentityDocumentBack, nameof(command.IdentityDocumentBack));
                AddIfMissing(missingDocuments, command.DrivingLicenseBack, nameof(command.DrivingLicenseBack));

                if (!HasAnyDamagePhoto(command))
                {
                    missingDocuments.Add("At least one windscreen damage photo");
                }
            }

            if (missingDocuments.Count > 0)
            {
                throw new ArgumentException($"Missing required document(s): {string.Join(", ", missingDocuments)}");
            }
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static void AddIfMissing(List<string> missingDocuments, string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                missingDocuments.Add(fieldName);
            }
        }

        private static bool HasAnyDamagePhoto(CreateClaimCommand command)
        {
            return !string.IsNullOrWhiteSpace(command.VehicleDamageFrontLeftDocument) ||
                   !string.IsNullOrWhiteSpace(command.VehicleDamageFrontRightDocument) ||
                   !string.IsNullOrWhiteSpace(command.VehicleDamageRearLeftDocument) ||
                   !string.IsNullOrWhiteSpace(command.VehicleDamageRearRightDocument);
        }

        private static void ValidateClaimTypeCoverage(CreateClaimCommand command, CoverageEntity coverage)
        {
            if (command.AllClaimType != AllClaimType.VehicleClaim || !command.MotorClaimType.HasValue)
            {
                return;
            }

            if (command.MotorClaimType == MotorClaimType.Windscreen &&
                coverage.WindscreenRemainingCoverageAmount <= 0m)
            {
                throw new ArgumentException("The selected coverage has no remaining windscreen coverage amount.");
            }

            if (command.MotorClaimType != MotorClaimType.Windscreen &&
                coverage.RemainingCoverageAmount <= 0m)
            {
                throw new ArgumentException("The selected coverage has no remaining comprehensive coverage amount.");
            }
        }

        private static void EnsureClaimIsNotWithdrawn(ClaimEntity claim)
        {
            if (string.Equals(claim.Status, "Withdrawn", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("A withdrawn claim cannot be processed further.");
            }
        }

        private async Task<ClaimEntity> GetExistingClaimAsync(Guid claimId)
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                throw new ArgumentException("Claim not found.");
            }

            return claim;
        }

        private async Task SendClaimCreatedNotificationAsync(ClaimEntity claim, string vehicleNo)
        {
            if (claim.IsFlaggedForManualReview)
            {
                await NotifyCustomerAsync(
                    claim,
                    "Your motor claim was submitted for manual review",
                    BuildClaimStatusEmailBody(
                        claim,
                        "Your claim has been submitted successfully, but it has been flagged for manual review.",
                        claim.ManualReviewFlagReason,
                        "An officer will review this claim before any approval decision is made."));
                return;
            }

            if (claim.IsSTPApproved || claim.STPStatus == StpStatus.AutoApproved)
            {
                await NotifyCustomerAsync(
                    claim,
                    "Your motor claim was submitted and auto-approved",
                    BuildClaimStatusEmailBody(
                        claim,
                        "Your claim has been submitted successfully and approved automatically by STP.",
                        null,
                        "You can proceed to choose a panel workshop in the system."));
                return;
            }

            await NotifyCustomerAsync(
                claim,
                "Your motor claim was submitted successfully",
                $"""
                <p>Your motor claim has been submitted successfully.</p>
                <p><strong>Claim ID:</strong> {claim.ClaimId}</p>
                <p><strong>Vehicle No:</strong> {WebUtility.HtmlEncode(vehicleNo)}</p>
                <p><strong>Status:</strong> {claim.Status}</p>
                <p>Your claim is now waiting for manual review. We will notify you again when there is an update.</p>
                """);
        }

        private async Task NotifyCustomerAsync(ClaimEntity claim, string subject, string htmlBody)
        {
            var customer = await _userRepository.GetByIdAsync(claim.UserId);
            if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
            {
                claim.EmailNotificationSent = false;
                claim.EmailNotificationMessage = "Customer email address was not found.";
                return;
            }

            var result = await _emailNotificationService.SendDiagnosticAsync(
                customer.Email,
                subject,
                WrapEmail(customer.FullName, htmlBody));

            claim.EmailNotificationSent = result.Success;
            claim.EmailNotificationMessage = result.Message;
        }

        private static string BuildClaimStatusEmailBody(
            ClaimEntity claim,
            string headline,
            string? note,
            string nextStep)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"<p>{headline}</p>");
            builder.AppendLine($"<p><strong>Claim ID:</strong> {claim.ClaimId}</p>");
            builder.AppendLine($"<p><strong>Status:</strong> {claim.Status}</p>");

            if (!string.IsNullOrWhiteSpace(note))
            {
                builder.AppendLine($"<p><strong>Note:</strong> {note}</p>");
            }

            builder.AppendLine($"<p>{nextStep}</p>");
            return builder.ToString();
        }

        private static string WrapEmail(string customerName, string content)
        {
            return $"""
                <div style="font-family: Arial, sans-serif; color: #1f2937; line-height: 1.6;">
                    <p>Hello {WebUtility.HtmlEncode(customerName)},</p>
                    {content}
                    <p>Regards,<br />Motor Claim System</p>
                </div>
                """;
        }
    }
}
