using Motor.Claim.Application.Features.Coverage.Commands;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Services
{
    public class CoverageService
    {
        private const decimal DefaultCoverageLimitAmount = 50000m;
        private const decimal DefaultWindscreenCoverageLimitAmount = 0m;
        private readonly ICoverageRepository _coverageRepository;

        public CoverageService(ICoverageRepository coverageRepository)
        {
            _coverageRepository = coverageRepository;
        }

        public async Task<CoverageEntity> CreateAsync(CreateCoverageCommand command)
        {
            if (command.ExpiryDate < command.EffectiveDate)
            {
                throw new ArgumentException("Expiry date cannot be earlier than effective date.");
            }

            if (command.CoverageLimitAmount is < 0)
            {
                throw new ArgumentException("Coverage limit amount cannot be negative.");
            }

            if (command.WindscreenCoverageLimitAmount is < 0)
            {
                throw new ArgumentException("Windscreen coverage limit amount cannot be negative.");
            }

            if (command.Year <= 0)
            {
                throw new ArgumentException("Vehicle year must be greater than zero.");
            }

            var coverageLimitAmount = command.CoverageLimitAmount ?? DefaultCoverageLimitAmount;
            var windscreenCoverageLimitAmount = command.WindscreenCoverageLimitAmount ?? DefaultWindscreenCoverageLimitAmount;

            var coverage = new CoverageEntity
            {
                CreatedAt = DateTime.Now,
                CoverageId = Guid.NewGuid(),
                UserId = command.UserId,
                InsuredPersonName = command.InsuredPersonName,
                VehicleNo = command.VehicleNo,
                VehicleMake = command.VehicleMake,
                VehicleModel = command.VehicleModel,
                Year = command.Year,
                ModelType = command.ModelType,
                CoverageType = command.CoverageType,
                AuthorizedDriver = command.AuthorizedDriver,
                EffectiveDate = command.EffectiveDate,
                ExpiryDate = command.ExpiryDate,
                CoverageLimitAmount = coverageLimitAmount,
                UsedClaimAmount = 0m,
                RemainingCoverageAmount = coverageLimitAmount,
                WindscreenCoverageLimitAmount = windscreenCoverageLimitAmount,
                WindscreenUsedClaimAmount = 0m,
                WindscreenRemainingCoverageAmount = windscreenCoverageLimitAmount
            };

            return await _coverageRepository.AddAsync(coverage);
        }

        public async Task<List<CoverageEntity>> GetByUserIdAsync(Guid userId)
        {
            return await _coverageRepository.GetByUserIdAsync(userId);
        }

        public async Task<List<CoverageEntity>> GetAllAsync()
        {
            return await _coverageRepository.GetAllWithClaimsAsync();
        }
    }
}
