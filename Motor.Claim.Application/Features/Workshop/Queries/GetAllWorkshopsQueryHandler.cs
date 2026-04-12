using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Services;

namespace Motor.Claim.Application.Features.Workshop.Queries
{
    public class GetAllWorkshopsQueryHandler
    {
        private readonly WorkshopService _workshopService;

        public GetAllWorkshopsQueryHandler(WorkshopService workshopService)
        {
            _workshopService = workshopService;
        }

        public async Task<List<WorkshopResponse>> Handle(GetAllWorkshopsQuery query)
        {
            return await _workshopService.GetAllWorkshopsAsync();
        }
    }
}
