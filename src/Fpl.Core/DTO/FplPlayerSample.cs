using FplClient.Data;

namespace Fpl.Core.DTO
{
    public class FplPlayerBasic
    {
        public int Id { get; set; }
        public string WebName { get; set; }
        public int TotalPoints { get; set; }
        public FplPlayerPosition Position { get; set; }
        public double Form { get; set; }
        public double Total { get; set; }
        public int NowCost { get; set; }
        public int PurchaseCost { get; set; }
        public int SellingCost { get; set; }
    }
}
