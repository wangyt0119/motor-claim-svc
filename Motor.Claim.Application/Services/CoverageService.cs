using Motor.Claim.Application.Features.Coverage.Commands;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Services
{
    public class CoverageService
    {
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

            var coverage = new CoverageEntity
            {
                CreatedAt = DateTime.Now,
                CoverageId = Guid.NewGuid(),
                UserId = command.UserId,
                InsuredPersonName = command.InsuredPersonName,
                VehicleNo = command.VehicleNo,
                CoverageType = command.CoverageType,
                EffectiveDate = command.EffectiveDate,
                ExpiryDate = command.ExpiryDate
            };

            return await _coverageRepository.AddAsync(coverage);
        }

        public async Task<List<CoverageEntity>> GetByUserIdAsync(Guid userId)
        {
            return await _coverageRepository.GetByUserIdAsync(userId);
        }
    }
}