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
        public string CoverageType { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
