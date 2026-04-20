using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IWorkshopPaymentProviderResolver
    {
        Task<WorkshopPaymentProviderResolution> ResolveAsync(WorkshopRepairEstimateEntity estimate);
    }

    public class WorkshopPaymentProviderResolution
    {
        public string Provider { get; set; } = "MockSandbox";
        public string ProviderReference { get; set; } = string.Empty;
        public string Status { get; set; } = "Paid";
    }
}
