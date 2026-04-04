using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Features.Coverage.Queries
{
    public class GetMyCoveragesQueryHandler
    {
        private readonly CoverageService _coverageService;

        public GetMyCoveragesQueryHandler(CoverageService coverageService)
        {
            _coverageService = coverageService;
        }

        public async Task<List<CoverageEntity>> Handle(GetMyCoveragesQuery query)
        {
            return await _coverageService.GetByUserIdAsync(query.UserId);
        }
    }
}