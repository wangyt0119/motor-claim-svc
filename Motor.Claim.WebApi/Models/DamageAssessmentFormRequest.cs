using Microsoft.AspNetCore.Http;

namespace Motor.Claim.WebApi.Models
{
    public class DamageAssessmentFormRequest
    {
        public IFormFile? Image { get; set; }
        public Guid? CoverageId { get; set; }
        public string VehicleMake { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public int Year { get; set; }
        public string ModelType { get; set; } = string.Empty;
        public string? CustomerMessage { get; set; }
    }
}
