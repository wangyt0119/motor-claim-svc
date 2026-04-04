using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Features.Coverage.Commands
{
    public class CreateCoverageCommandHandler
    {
        private readonly CoverageService _coverageService;

        public CreateCoverageCommandHandler(CoverageService coverageService)
        {
            _coverageService = coverageService;
        }

        public async Task<CoverageEntity> Handle(CreateCoverageCommand command)
        {
            return await _coverageService.CreateAsync(command);
        }
    }
}