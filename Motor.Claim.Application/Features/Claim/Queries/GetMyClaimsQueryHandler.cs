using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Features.Claim.Queries
{
    public class GetMyClaimsQueryHandler
    {
        private readonly ClaimService _claimService;

        public GetMyClaimsQueryHandler(ClaimService claimService)
        {
            _claimService = claimService;
        }

        public async Task<List<ClaimEntity>> Handle(GetMyClaimsQuery query)
        {
            return await _claimService.GetByUserIdAsync(query.UserId);
        }
    }
}