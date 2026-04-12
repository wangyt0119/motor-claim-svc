namespace Motor.Claim.Application.Dtos.Claim
{
    public class CustomerResponseRequest
    {
        public string? ResponseNote { get; set; }
        public List<string> ResponseDocuments { get; set; } = new();
    }
}
