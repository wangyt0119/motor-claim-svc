using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Features.Coverage.Queries
{
    public class GetAllCoveragesQueryHandler
    {
        private readonly CoverageService _coverageService;

        public GetAllCoveragesQueryHandler(CoverageService coverageService)
        {
            _coverageService = coverageService;
        }

        public async Task<List<CoverageEntity>> Handle(GetAllCoveragesQuery query)
        {
            return await _coverageService.GetAllAsync();
        }
    }
}
