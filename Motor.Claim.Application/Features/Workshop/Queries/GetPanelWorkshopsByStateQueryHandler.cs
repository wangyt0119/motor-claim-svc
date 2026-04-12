using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Services;

namespace Motor.Claim.Application.Features.Workshop.Queries
{
    public class GetPanelWorkshopsByStateQueryHandler
    {
        private readonly WorkshopService _workshopService;

        public GetPanelWorkshopsByStateQueryHandler(WorkshopService workshopService)
        {
            _workshopService = workshopService;
        }

        public async Task<List<WorkshopResponse>> Handle(GetPanelWorkshopsByStateQuery query)
        {
            return await _workshopService.GetPanelWorkshopsByStateAsync(query.State);
        }
    }
}
