using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Workshop
{
    public class UpdateMyWorkshopRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string State { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public List<string> Phone { get; set; } = new();
        public string? Fax { get; set; }
        public List<string> Email { get; set; } = new();
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolderName { get; set; }
    }
}
