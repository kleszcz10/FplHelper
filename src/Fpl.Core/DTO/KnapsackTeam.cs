using System.Collections.Generic;
using System.Linq;

namespace Fpl.Core.DTO
{
    public class KnapsackTeam
    {
        public IEnumerable<FplPlayerExtension> Team { get; set; }
        public double? SumIndex => Team?.Sum(x => x.BackpackIndex);
        public double? TotalCost => Team?.Sum(x => x.NowCost);
        public int? TotalPoints => Team?.Sum(x => x.TotalPoints);

        public int NumberOfGeneratedTeams { get; internal set; }
    }
}
