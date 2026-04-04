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

        public ClaimService(IClaimRepository claimRepository, ICoverageRepository coverageRepository)
        {
            _claimRepository = claimRepository;
            _coverageRepository = coverageRepository;
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
                Status = "Pending"
            };

            return await _claimRepository.AddAsync(claim);
        }

        public async Task<List<ClaimEntity>> GetByUserIdAsync(Guid userId)
        {
            return await _claimRepository.GetByUserIdAsync(userId);
        }

        private static void ValidateDocuments(CreateClaimCommand command)
        {
            var missingDocuments = new List<string>();

            AddIfMissing(missingDocuments, command.PoliceReportDocument, nameof(command.PoliceReportDocument));
            AddIfMissing(missingDocuments, command.IdentityDocumentFront, nameof(command.IdentityDocumentFront));
            AddIfMissing(missingDocuments, command.IdentityDocumentBack, nameof(command.IdentityDocumentBack));

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
                AddIfMissing(missingDocuments, command.DrivingLicenseBack, nameof(command.DrivingLicenseBack));
                AddIfMissing(missingDocuments, command.VehicleDamageFrontLeftDocument, nameof(command.VehicleDamageFrontLeftDocument));
                AddIfMissing(missingDocuments, command.VehicleDamageFrontRightDocument, nameof(command.VehicleDamageFrontRightDocument));
                AddIfMissing(missingDocuments, command.VehicleDamageRearLeftDocument, nameof(command.VehicleDamageRearLeftDocument));
                AddIfMissing(missingDocuments, command.VehicleDamageRearRightDocument, nameof(command.VehicleDamageRearRightDocument));
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
    }
}
