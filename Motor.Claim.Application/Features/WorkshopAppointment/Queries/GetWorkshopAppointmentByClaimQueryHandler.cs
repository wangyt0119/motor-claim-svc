using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Services;

namespace Motor.Claim.Application.Features.WorkshopAppointment.Queries
{
    public class GetWorkshopAppointmentByClaimQueryHandler
    {
        private readonly WorkshopService _workshopService;

        public GetWorkshopAppointmentByClaimQueryHandler(WorkshopService workshopService)
        {
            _workshopService = workshopService;
        }

        public async Task<WorkshopAppointmentResponse?> Handle(GetWorkshopAppointmentByClaimQuery query)
        {
            return await _workshopService.GetAppointmentByClaimAsync(query.UserId, query.ClaimId, query.EnforceOwnership);
        }
    }
}
