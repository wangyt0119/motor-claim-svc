using Motor.Claim.Application.Services;

namespace Motor.Claim.Application.Features.Workshop.Commands
{
    public class DeleteWorkshopCommandHandler
    {
        private readonly WorkshopService _workshopService;

        public DeleteWorkshopCommandHandler(WorkshopService workshopService)
        {
            _workshopService = workshopService;
        }

        public async Task Handle(DeleteWorkshopCommand command)
        {
            await _workshopService.DeleteWorkshopAsync(command.WorkshopId);
        }
    }
}
