using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Features.Workshop.Commands
{
    public class UpdateWorkshopCommandHandler
    {
        private readonly WorkshopService _workshopService;

        public UpdateWorkshopCommandHandler(WorkshopService workshopService)
        {
            _workshopService = workshopService;
        }

        public async Task<WorkshopEntity> Handle(UpdateWorkshopCommand command)
        {
            return await _workshopService.UpdateWorkshopAsync(command);
        }
    }
}
