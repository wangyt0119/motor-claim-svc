namespace Motor.Claim.Application.Features.Workshop.Commands
{
    public class CreateWorkshopCommand
    {
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<string> Phone { get; set; } = new();
        public string? Fax { get; set; }
        public List<string> Email { get; set; } = new();
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolderName { get; set; }
        public bool IsPanelWorkshop { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}
