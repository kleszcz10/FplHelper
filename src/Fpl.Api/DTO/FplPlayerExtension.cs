using FplClient.Data;

namespace Fpl.Api.Controllers
{
    public class FplPlayerExtension : FplPlayer
    {
        public float BackpackIndex { get; set; }
        public double NextGwFixture { get; set; }
        public double AvgOfNext2GwFixture { get; set; }
        public double AvgOfNext3GwFixture { get; set; }
        public double AvgOfNext4GwFixture { get; set; }
    }
}
