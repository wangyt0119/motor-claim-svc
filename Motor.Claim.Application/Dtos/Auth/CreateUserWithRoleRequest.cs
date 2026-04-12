using Motor.Claim.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Auth
{
    public class CreateUserWithRoleRequest : RegisterUserRequest
    {
        [Required]
        public UserRole Role { get; set; }

        public Guid? WorkshopId { get; set; }
    }
}
