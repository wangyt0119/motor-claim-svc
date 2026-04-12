using Motor.Claim.Application.Services;

namespace Motor.Claim.Application.Features.Workshop.Queries
{
    public class GetPanelWorkshopStatesQueryHandler
    {
        private readonly WorkshopService _workshopService;

        public GetPanelWorkshopStatesQueryHandler(WorkshopService workshopService)
        {
            _workshopService = workshopService;
        }

        public async Task<List<string>> Handle(GetPanelWorkshopStatesQuery query)
        {
            return await _workshopService.GetPanelStatesAsync();
        }
    }
}
