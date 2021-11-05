using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fpl.Api.DTO
{
    public class Parameter
    {
        public string PropertyName { get; set; }
        public int Weight { get; set; }
        public bool Ascending { get; set; }
        public bool DisplayOnly { get; set; }
    }
}
