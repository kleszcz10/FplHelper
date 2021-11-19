using Fpl.Core.DTO;
using Fpl.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public Parameters GetParameters()
        {
            var parameters = new Parameters();
            var properties = typeof(FplPlayerExtension).GetProperties();

            parameters.NumericParameters = properties.Where(x =>
            {
                switch (Type.GetTypeCode(x.PropertyType))
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Single:
                        return true;
                    default:
                        return false;
                }
            }).Select(x => x.Name).ToArray();

            parameters.DisplayOnlyParameters = properties.Select(x => x.Name)
                                                         .Where(x => !parameters.NumericParameters.Contains(x))
                                                         .ToArray();

            return parameters;
        }
        [HttpPost("{teamId}/{numberOfPlayersToReplace}")]
        public Task<IEnumerable<ReplaceProposition>> ReplaceInMyTeam(int teamId, int numberOfPlayersToReplace, List<OptimalisationParameter> parameters)
        {
            return _fplService.ReplaceInMyTeam(teamId, numberOfPlayersToReplace, parameters);
        }
        [HttpPost("{teamId}")]
        public Task<IEnumerable<PickTeamResult>> PickTeam(int teamId, List<OptimalisationParameter> parameters)
        {
            return _fplService.PickTeam(teamId, parameters);
        }
    }
}
