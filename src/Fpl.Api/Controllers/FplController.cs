using Fpl.Api.DTO;
using Fpl.Api.Services;
using Fpl.Api.Tools;
using FplClient.Clients;
using FplClient.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nelibur.ObjectMapper;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Fpl.Api.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    public class FplController : ControllerBase
    {
        private readonly IFplService _fplService;
        public FplController(IFplService fplService)
        {
            _fplService = fplService ?? throw new ArgumentNullException(nameof(fplService));
        }
        [HttpPost]
        public async Task<BestTeamResult> Optimalisation(OptimalisationParameter[] parameters)
        {
            var players = await _fplService.GetPlayers();

            return _fplService.PlayersOptimalisation(parameters, players);
        }
        [HttpPost("{timeout}")]
        public Task<KnapsackTeam> KnapsackTeamBasedOnOptimalisation(int timeout, List<OptimalisationParameter> parameters, CancellationToken cancellationToken)
        {
            return _fplService.KnapsackTeamBasedOnOptimalisation(timeout, parameters, cancellationToken);
        }

        [HttpGet]
        public IEnumerable<string> GetParameters()
        {
            return typeof(FplPlayerExtension).GetProperties().Select(x => x.Name);
        }
        [HttpPost("{teamId}/{numberOfPlayersToReplace}")]
        public Task<IEnumerable<ReplaceProposition>> ReplaceInMyTeam(int teamId, int numberOfPlayersToReplace, List<OptimalisationParameter> parameters)
        {
            return _fplService.ReplaceInMyTeam(teamId, numberOfPlayersToReplace, parameters);
        }
     
    }
}
