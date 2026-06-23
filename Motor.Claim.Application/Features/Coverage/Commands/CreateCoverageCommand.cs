using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Motor.Claim.Application.Features.Coverage.Commands
{
    public class CreateCoverageCommand
    {
        public Guid UserId { get; set; }
        public string InsuredPersonName { get; set; }
        public string VehicleNo { get; set; }
        public string VehicleMake { get; set; }
        public string VehicleModel { get; set; }
        public int Year { get; set; }
        public string ModelType { get; set; }
        public string CoverageType { get; set; }
        public string? AuthorizedDriver { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal? CoverageLimitAmount { get; set; }
        public decimal? WindscreenCoverageLimitAmount { get; set; }
    }
}
