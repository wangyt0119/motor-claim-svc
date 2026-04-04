using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Features.Claim.Commands
{
    public class CreateClaimCommandHandler
    {
        private readonly ClaimService _claimService;

        public CreateClaimCommandHandler(ClaimService claimService)
        {
            _claimService = claimService;
        }

        public async Task<ClaimEntity> Handle(CreateClaimCommand command)
        {
            return await _claimService.CreateAsync(command);
        }
    }
}