using System.Text.Json;
using Motor.Claim.Application.Features.Claim.Commands;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Services
{
    public class ClaimService
    {
        private readonly IClaimRepository _claimRepository;
        private readonly ICoverageRepository _coverageRepository;
        private readonly StpValidationService _stpValidationService;

        public ClaimService(
            IClaimRepository claimRepository,
            ICoverageRepository coverageRepository,
            StpValidationService stpValidationService)
        {
            _claimRepository = claimRepository;
            _coverageRepository = coverageRepository;
            _stpValidationService = stpValidationService;
        }

        public async Task<ClaimEntity> CreateAsync(CreateClaimCommand command)
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

            if (command.IncidentDate.Date < coverage.EffectiveDate.Date || command.IncidentDate.Date > coverage.ExpiryDate.Date)
            {
                throw new ArgumentException("Incident date must be between the coverage effective date and expiry date.");
            }

            ValidateDocuments(command);

            var claim = new ClaimEntity
            {
                CreatedAt = DateTime.UtcNow,
                ClaimId = Guid.NewGuid(),
                UserId = command.UserId,
                CoverageId = command.CoverageId,
                IncidentDate = command.IncidentDate,
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
                Status = "Pending",
                ReviewStatus = "Pending"
            };

            var savedClaim = await _claimRepository.AddAsync(claim);

            var stpResult = await _stpValidationService.ValidateAsync(savedClaim, coverage);

            savedClaim.STPStatus = stpResult.STPStatus;
            savedClaim.IsSTPApproved = stpResult.IsApproved;
            savedClaim.ValidationResult = StpValidationService.SerializeResult(stpResult);
            savedClaim.Status = stpResult.IsApproved ? "Approved" : "Pending Manual Review";
            savedClaim.ReviewStatus = stpResult.IsApproved ? "Approved" : "PendingManualReview";
            savedClaim.DecidedAt = stpResult.IsApproved ? DateTime.UtcNow : null;

            await _claimRepository.UpdateAsync(savedClaim);

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
            claim.Status = "Approved";
            claim.ReviewStatus = "Approved";
            claim.OfficerDecisionNote = note;
            claim.DecidedAt = DateTime.UtcNow;
            claim.ReviewedByUserId = officerUserId;

            await _claimRepository.UpdateAsync(claim);
            return claim;
        }

        public async Task<ClaimEntity> RejectAsync(Guid claimId, Guid officerUserId, string? note)
        {
            var claim = await GetExistingClaimAsync(claimId);
            claim.Status = "Rejected";
            claim.ReviewStatus = "Rejected";
            claim.OfficerDecisionNote = note;
            claim.DecidedAt = DateTime.UtcNow;
            claim.ReviewedByUserId = officerUserId;

            await _claimRepository.UpdateAsync(claim);
            return claim;
        }

        public async Task<ClaimEntity> RequestInfoAsync(Guid claimId, Guid officerUserId, string requestedItems, string? note)
        {
            if (string.IsNullOrWhiteSpace(requestedItems))
            {
                throw new ArgumentException("RequestedItems is required.");
            }

            var claim = await GetExistingClaimAsync(claimId);
            claim.Status = "Pending Customer Action";
            claim.ReviewStatus = "PendingCustomerAction";
            claim.RequestedItems = requestedItems;
            claim.OfficerDecisionNote = note;
            claim.RequestedAt = DateTime.UtcNow;
            claim.DecidedAt = null;
            claim.ReviewedByUserId = officerUserId;

            await _claimRepository.UpdateAsync(claim);
            return claim;
        }

        public async Task<ClaimEntity> SubmitCustomerResponseAsync(Guid claimId, Guid userId, string? responseNote, List<string> responseDocuments)
        {
            var claim = await GetExistingClaimAsync(claimId);

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
            return claim;
        }

        private static void ValidateDocuments(CreateClaimCommand command)
        {
            var missingDocuments = new List<string>();

            AddIfMissing(missingDocuments, command.PoliceReportDocument, nameof(command.PoliceReportDocument));
            AddIfMissing(missingDocuments, command.IdentityDocumentFront, nameof(command.IdentityDocumentFront));

            if (command.AllClaimType != AllClaimType.VehicleClaim)
            {
                return;
            }

            if (!command.MotorClaimType.HasValue)
            {
                throw new ArgumentException("MotorClaimType is required when AllClaimType is VehicleClaim.");
            }

            if (command.MotorClaimType == MotorClaimType.VehicleDamages)
            {
                AddIfMissing(missingDocuments, command.VehicleOwnershipCertificateDocument, nameof(command.VehicleOwnershipCertificateDocument));
                AddIfMissing(missingDocuments, command.DrivingLicenseFront, nameof(command.DrivingLicenseFront));
            }

            if (missingDocuments.Count > 0)
            {
                throw new ArgumentException($"Missing required document(s): {string.Join(", ", missingDocuments)}");
            }
        }

        private static void AddIfMissing(List<string> missingDocuments, string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                missingDocuments.Add(fieldName);
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
    }
}
