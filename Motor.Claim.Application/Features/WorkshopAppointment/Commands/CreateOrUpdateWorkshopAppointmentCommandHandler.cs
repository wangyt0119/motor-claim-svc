using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Services;

namespace Motor.Claim.Application.Features.WorkshopAppointment.Commands
{
    public class CreateOrUpdateWorkshopAppointmentCommandHandler
    {
        private readonly WorkshopService _workshopService;

        public CreateOrUpdateWorkshopAppointmentCommandHandler(WorkshopService workshopService)
        {
            _workshopService = workshopService;
        }

        public async Task<WorkshopAppointmentResponse> Handle(CreateOrUpdateWorkshopAppointmentCommand command)
        {
            return await _workshopService.CreateOrUpdateAppointmentAsync(
                command.UserId,
                new CreateWorkshopAppointmentRequest
                {
                    ClaimId = command.ClaimId,
                    WorkshopId = command.WorkshopId,
                    PreferredDate = command.PreferredDate,
                    TimeSlotStart = command.TimeSlotStart,
                    TimeSlotEnd = command.TimeSlotEnd,
                    Notes = command.Notes
                });
        }
    }
}
