using System.Collections.Generic;
using Fpl.Api.DTO;
using Fpl.Api.Controllers;

namespace Fpl.Api.DTO
{
    public class ReplaceProposition
    {
        public List<FplPlayerBasic> Current { get; set; }
        public List<FplPlayerBasic> Alternative { get; set; }

    }
}
