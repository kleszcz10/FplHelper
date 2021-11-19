using Fpl.Core.DTO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fpl.Core.Services
{
    public interface IFplService
    {
        Task<KnapsackTeam> KnapsackTeamBasedOnOptimalisation(int timeout, List<OptimalisationParameter> parameters, CancellationToken cancellationToken);
        Task<IEnumerable<FplPlayerExtension>> GetPlayers();
        BestTeamResult PlayersOptimalisation(OptimalisationParameter[] parameters, IEnumerable<FplPlayerExtension> players);
        Task<IEnumerable<ReplaceProposition>> ReplaceInMyTeam(int teamId, int numberOfPlayersToReplace, List<OptimalisationParameter> parameters);
        Task<IEnumerable<PickTeamResult>> PickTeam(int teamId, List<OptimalisationParameter> parameters);
    }
}