namespace Fpl.Api.Controllers
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
    }
}
