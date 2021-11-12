using System.Collections.Generic;
using System.Linq;

namespace Fpl.Api.DTO
{
    public class PickTeamResult
    {
        public string Formation { get; set; }
        public double? PitchSumOfTotal => OnThePitch?.Sum(x => x.Total);
        public double? BenchSumOfTotal => OnTheBench?.Sum(x => x.Total);

        public IEnumerable<FplPlayerBasic> OnThePitch { get; set; }
        public IEnumerable<FplPlayerBasic> OnTheBench { get; set; }
        public IEnumerable<FplPlayerBasic> Capitans => OnThePitch.OrderByDescending(x => x.Total).Take(2);

    }
}
