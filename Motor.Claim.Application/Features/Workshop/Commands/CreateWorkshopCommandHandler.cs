using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Features.Workshop.Commands
{
    public class CreateWorkshopCommandHandler
    {
        private readonly WorkshopService _workshopService;

        public CreateWorkshopCommandHandler(WorkshopService workshopService)
        {
            _workshopService = workshopService;
        }

        public async Task<WorkshopEntity> Handle(CreateWorkshopCommand command)
        {
            return await _workshopService.CreateWorkshopAsync(command);
        }
    }
}
