namespace Motor.Claim.Application.Dtos.Claim
{
    public class RequestInfoRequest
    {
        public string RequestedItems { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
