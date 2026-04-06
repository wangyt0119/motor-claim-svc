using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Features.Claim.Queries
{
    public class GetAllClaimsQueryHandler
    {
        private readonly ClaimService _claimService;

        public GetAllClaimsQueryHandler(ClaimService claimService)
        {
            _claimService = claimService;
        }

        public async Task<List<ClaimEntity>> Handle(GetAllClaimsQuery query)
        {
            return await _claimService.GetAllAsync();
        }
    }
}
