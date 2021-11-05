using Fpl.Api.Controllers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fpl.Api.DTO
{
    public class Combination
    {
        public Combination()
        {
            players = new ConcurrentBag<FplPlayerExtension>();
        }
        public ConcurrentBag<FplPlayerExtension> players { get; set; }
        public float PointsSum => players.Sum(x => x?.TotalPoints ?? 0);

        public float ValueSum => players.Sum(x => x?.NowCost ?? 0);
        public double SellSum => players.Sum(x => x?.CostChangeStartFall ?? 0);

        public float AproxValue => PointsSum / ValueSum;

        public object LockObject = new object();

        //public int FixtureSum => players.Sum(x => x.Fixture);
        //public double NextThreeMatchesFixtureAvg => players.Average(x => x.NextThreeMatchesFixture);
        public int SumChanceOfPlayingInNextRound => players.Sum(x => int.Parse(x.ChanceOfPlayingNextRound));
        public double FormAvg => players.Average(x => x.Form);
    }
}
