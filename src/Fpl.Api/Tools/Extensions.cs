using Fpl.Api.Controllers;
using Fpl.Api.DTO;
namespace Fpl.Api.Tools
{
    public static class Extensions
    {
        public static double Normalise(double value, double min, double max)
        {
            return (value - min) / (max - min);
        }

        public static object GetValueByName(this FplPlayerExtension player, string propertyName)
        {
            return player.GetType().GetProperty(propertyName)?.GetValue(player);
        }

        public static FplPlayerBasic MapToBasic(this FplPlayerExtension player)
        {
            return new FplPlayerBasic
            {
                Id = player.Id,
                WebName = player.WebName,
                TotalPoints = player.TotalPoints,
                Position = player.Position,
                Form = player.Form,
                NowCost = player.NowCost,
                Total = player.Total
            };
        }
    }
}
