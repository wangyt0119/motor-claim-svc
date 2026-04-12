using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IWorkshopRepository : IGenericRepository<WorkshopEntity>
    {
        Task<List<string>> GetActivePanelStatesAsync();
        Task<List<WorkshopEntity>> GetActivePanelWorkshopsByStateAsync(string state);
    }
}
