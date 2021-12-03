using FplClient.Data;

namespace Fpl.Core.DTO
{
    public class FplPlayerExtension : FplPlayer
    {
        public double Total { get; set; }
        public float BackpackIndex { get; set; }
        public double NextGwFixture { get; set; }
        public double AvgOfNext2GwFixture { get; set; }
        public double AvgOfNext3GwFixture { get; set; }
        public double AvgOfNext4GwFixture { get; set; }
        public int ChanceOfPlayingInNextRound => string.IsNullOrEmpty(base.ChanceOfPlayingNextRound) ? 100 : int.Parse(base.ChanceOfPlayingNextRound);
    }
}
