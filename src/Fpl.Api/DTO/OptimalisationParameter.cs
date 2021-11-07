using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fpl.Api.DTO
{
    public class OptimalisationParameter
    {
        public string PropertyName { get; set; }
        public int Weight { get; set; }
        public bool Ascending { get; set; }
        public bool DisplayOnly { get; set; }
    }
}
