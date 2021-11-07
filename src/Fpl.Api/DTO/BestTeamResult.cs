using System.Collections.Generic;

namespace Fpl.Api.DTO
{
    public class BestTeamResult
    {
        public Dictionary<string,Dictionary<string,object>> Parameters { get; set; }
        public ICollection<Dictionary<string,object>> Results { get; set; }
    }
}
