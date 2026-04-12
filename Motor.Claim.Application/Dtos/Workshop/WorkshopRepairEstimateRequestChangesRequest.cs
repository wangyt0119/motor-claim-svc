namespace Motor.Claim.Application.Dtos.Workshop
{
    public class WorkshopRepairEstimateRequestChangesRequest
    {
        public string RequestedItems { get; set; } = string.Empty;
        public string? ReviewNote { get; set; }
    }
}
