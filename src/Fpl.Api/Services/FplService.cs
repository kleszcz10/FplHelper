using Fpl.Api.Controllers;
using Fpl.Api.DTO;
using FplClient.Clients;
using FplClient.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nelibur.ObjectMapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Api.Tools;
using FplClient;

namespace Fpl.Api.Services
{
    public class FplService : IFplService
    {
        private readonly IFplEntryClient _fplEntryClient;
        private readonly IFplEntryHistoryClient _fplEntryHistoryClient;
        private readonly IFplLeagueClient _fplLeagueClient;
        private readonly IFplPlayerClient _fplPlayerClient;
        private readonly IFplFixtureClient _fplFixtureClient;
        private readonly IFplGameweekClient _fplGameweekClient;
        private readonly IFplGlobalSettingsClient _fplGlobalSettingsClient;
        private readonly IFplLiveGameweekStatsClient _fplLiveGameweekStatsClient;
        private readonly ILogger<FplService> _logger;
        private readonly IMemoryCache _cache;

        public FplService(IFplEntryClient fplEntryClient,
                          IFplEntryHistoryClient fplEntryHistoryClient,
                          IFplLeagueClient fplLeagueClient,
                          IFplPlayerClient fplPlayerClient,
                          IFplFixtureClient fplFixtureClient,
                          IFplGameweekClient fplGameweekClient,
                          IFplGlobalSettingsClient fplGlobalSettingsClient,
                          IFplLiveGameweekStatsClient fplLiveGameweekStatsClient,
                          ILogger<FplService> logger,
                          IMemoryCache cache)
        {
            _fplEntryClient = fplEntryClient ?? throw new ArgumentNullException(nameof(fplEntryClient));
            _fplEntryHistoryClient = fplEntryHistoryClient ?? throw new ArgumentNullException(nameof(fplEntryHistoryClient));
            _fplLeagueClient = fplLeagueClient ?? throw new ArgumentNullException(nameof(fplLeagueClient));
            _fplPlayerClient = fplPlayerClient ?? throw new ArgumentNullException(nameof(fplPlayerClient));
            _fplFixtureClient = fplFixtureClient ?? throw new ArgumentNullException(nameof(fplFixtureClient));
            _fplGameweekClient = fplGameweekClient ?? throw new ArgumentNullException(nameof(fplGameweekClient));
            _fplGlobalSettingsClient = fplGlobalSettingsClient ?? throw new ArgumentNullException(nameof(fplGlobalSettingsClient));
            _fplLiveGameweekStatsClient = fplLiveGameweekStatsClient ?? throw new ArgumentNullException(nameof(fplLiveGameweekStatsClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }
        public BestTeamResult PlayersOptimalisation(OptimalisationParameter[] parameters, IEnumerable<FplPlayerExtension> players)
        {
            var result = new BestTeamResult();
            result.Parameters = new Dictionary<string, Dictionary<string, object>>();
            result.Results = new List<Dictionary<string, object>>();

            var weightPropertyName = "weight";
            var minPropertyName = "min";
            var maxPropertyName = "max";

            #region Prepare input data
            foreach (var parameter in parameters.Where(x => x.DisplayOnly == false))
            {
                var values = new Dictionary<string, object>();

                var min = players.Min(x => x.GetValueByName(parameter.PropertyName) ?? 0);
                var max = players.Max(x => x.GetValueByName(parameter.PropertyName) ?? 0);

                values.Add(minPropertyName, min);
                values.Add(maxPropertyName, max);
                values.Add(weightPropertyName, parameter.Weight);

                result.Parameters.Add(parameter.PropertyName, values);
            }

            var sumOfWeights = result.Parameters.Sum(x => Convert.ToDouble(x.Value[weightPropertyName]));

            foreach (var input in result.Parameters)
            {
                var weight = input.Value[weightPropertyName];
                double normalizeWeight = Convert.ToDouble(weight) / sumOfWeights;

                input.Value[weightPropertyName] = double.IsNaN(normalizeWeight) ? 0 : normalizeWeight;
            }
            #endregion
            #region PrepareResult
            foreach (var player in players)
            {
                var playerResult = new Dictionary<string, object>();
                double total = 0;

                foreach (var parameter in parameters.Where(x => x.DisplayOnly))
                {
                    playerResult.Add(parameter.PropertyName, player.GetValueByName(parameter.PropertyName));
                }

                foreach (var parameter in parameters.Where(x => x.DisplayOnly == false))
                {
                    var min = result.Parameters[parameter.PropertyName][minPropertyName];
                    var max = result.Parameters[parameter.PropertyName][maxPropertyName];
                    var weight = result.Parameters[parameter.PropertyName][weightPropertyName];

                    var currentValue = player.GetValueByName(parameter.PropertyName);
                    var normalise = Extensions.Normalise(Convert.ToDouble(currentValue), Convert.ToDouble(min), Convert.ToDouble(max));

                    if (parameter.Ascending == false)
                    {
                        normalise = 1 - normalise;
                    }

                    playerResult.Add(parameter.PropertyName, currentValue);

                    total += (normalise * Convert.ToDouble(weight));
                }

                playerResult.Add("Total", total);

                result.Results.Add(playerResult);
            }

            #endregion

            result.Results = result.Results.OrderByDescending(x => x["Total"]).ToList();
            return result;
        }

        public async Task<IEnumerable<FplPlayerExtension>> GetPlayers()
        {

            if (!_cache.TryGetValue(nameof(GetPlayers), out List<FplPlayerExtension> cast))
            {
                var players = await _fplPlayerClient.GetAllPlayers();
                var fixtures = await _fplFixtureClient.GetFixtures();
                var gameweeks = await _fplGameweekClient.GetGameweeks();
                var currentGameweek = gameweeks.FirstOrDefault(x => x.IsCurrent);
                var settings = await _fplGlobalSettingsClient.GetGlobalSettings();

                var fixturesData = new Dictionary<int, Dictionary<int, double>>();

                var gameweeksRange = Enumerable.Range(currentGameweek.Id, currentGameweek.Id + 5);
                var fixturesInRange = fixtures.Where(x => x.Event.HasValue && gameweeksRange.Contains(x.Event.Value));

                foreach (var team in settings.Teams)
                {
                    var fixturesHome = fixturesInRange.Where(x => x.HomeTeamId == team.Id).Select(x => new { eventId = x.Event.Value, fixture = x.HomeTeamDifficulty });
                    var fixturesAway = fixturesInRange.Where(x => x.AwayTeamId == team.Id).Select(x => new { eventId = x.Event.Value, fixture = x.AwayTeamDifficulty });
                    var teamFixtures = fixturesHome.Concat(fixturesAway).GroupBy(x => x.eventId).ToDictionary(x => x.Key, x => x.Average(a => a.fixture));
                    fixturesData.Add((int)team.Id, teamFixtures);
                }

                TinyMapper.Bind<FplPlayer, FplPlayerExtension>();

                cast = players.Select(x => TinyMapper.Map<FplPlayer, FplPlayerExtension>(x)).ToList();


                foreach (var player in cast)
                {
                    var playerFixtures = fixturesData[player.TeamId];
                    player.NextGwFixture = playerFixtures[currentGameweek.Id];
                    player.AvgOfNext2GwFixture = playerFixtures.Where(x => x.Key == currentGameweek.Id || x.Key == currentGameweek.Id + 1).Average(x => x.Value);
                    player.AvgOfNext3GwFixture = playerFixtures.Where(x => x.Key == currentGameweek.Id || x.Key == currentGameweek.Id + 1 || x.Key == currentGameweek.Id + 3).Average(x => x.Value);
                    player.AvgOfNext4GwFixture = playerFixtures.Where(x => x.Key == currentGameweek.Id || x.Key == currentGameweek.Id + 1 || x.Key == currentGameweek.Id + 3 || x.Key == currentGameweek.Id + 4).Average(x => x.Value);
                }

                _cache.Set(nameof(GetPlayers), cast, TimeSpan.FromHours(1));
            }

            return cast;
        }

        public async Task<KnapsackTeam> KnapsackTeamBasedOnOptimalisation(int timeout, List<OptimalisationParameter> parameters, CancellationToken cancellationToken)
        {
            if (parameters.Any(x => x.PropertyName != nameof(FplPlayer.Id)))
            {
                parameters.Add(new OptimalisationParameter
                {
                    PropertyName = nameof(FplPlayer.Id),
                    DisplayOnly = true
                });
            }

            var players = await GetPlayers();

            var optimalisation = PlayersOptimalisation(parameters.ToArray(), players);

            players = players.Select(x =>
            {

                var optimalPlayer = optimalisation.Results.FirstOrDefault(o => Convert.ToInt32(o[nameof(FplPlayer.Id)]) == x.Id);
                var total = Convert.ToDouble(optimalPlayer["Total"]);

                x.BackpackIndex = (float)x.NowCost / (float)total;

                return x;

            }).ToList();

            players = players.Where(x => x.BackpackIndex < players.Average(c => c.BackpackIndex)).OrderBy(x => x.BackpackIndex).ToList();

            Func<IEnumerable<FplPlayerExtension>, bool> condition = (team) => team.Sum(p => p.NowCost) <= 1000
                                                         && team.GroupBy(p => p.TeamId).All(p => p.Count() < 4
                                                         && team.GroupBy(p => p.Id).All(p => p.Count() == 1));


            var canncellationTokenSource = new CancellationTokenSource();

            var timer = new System.Timers.Timer
            {
                AutoReset = false,
                Interval = TimeSpan.FromSeconds(timeout).TotalMilliseconds,
            };

            cancellationToken.Register(() => canncellationTokenSource.Cancel());


            timer.Elapsed += (x, y) => canncellationTokenSource.Cancel();



            var bestTeams = new ConcurrentBag<IEnumerable<FplPlayerExtension>>();

            var pararellOptions = new ParallelOptions
            {
                CancellationToken = canncellationTokenSource.Token
            };

            timer.Start();

            try
            {

                Backpack.GetPermutations(players.Where(x => x.Position == FplPlayerPosition.Goalkeeper).ToList(), 2)
                        .AsParallel()
                        .WithCancellation(canncellationTokenSource.Token)
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .ForAll(goalkeepers =>
                        {
                            try
                            {
                                Backpack.GetPermutations(players.Where(x => x.Position == FplPlayerPosition.Forward).ToList(), 3)
                                         .AsParallel()
                                         .WithCancellation(canncellationTokenSource.Token)
                                         .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                         .ForAll(forwarders =>
                                         {
                                             try
                                             {
                                                 Backpack.GetPermutations(players.Where(x => x.Position == FplPlayerPosition.Midfielder).ToList(), 5)
                                                          .AsParallel()
                                                          .WithCancellation(canncellationTokenSource.Token)
                                                          .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                                          .ForAll(midlefielders =>
                                                          {
                                                              try
                                                              {
                                                                  Backpack.GetPermutations(players.Where(x => x.Position == FplPlayerPosition.Defender).ToList(), 5)
                                                                          .AsParallel()
                                                                          .WithCancellation(canncellationTokenSource.Token)
                                                                          .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                                                          .ForAll(defenders =>
                                                                          {
                                                                              var team = goalkeepers.Concat(forwarders).Concat(midlefielders).Concat(defenders);
                                                                              if (condition.Invoke(team))
                                                                              {
                                                                                  _logger.LogInformation(team.Sum(p => p.TotalPoints).ToString());
                                                                                  bestTeams.Add(team);
                                                                              }
                                                                          });
                                                              }
                                                              catch
                                                              {
                                                              }
                                                          });
                                             }
                                             catch
                                             {
                                             }
                                         });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.Message);
                            }
                        });
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex.Message); 
            }
            finally
            {
                canncellationTokenSource.Dispose();
                timer.Stop();
                timer.Dispose();
            }
            return new KnapsackTeam
            {
                Team = bestTeams.OrderBy(x => x.Sum(p => p.BackpackIndex)).FirstOrDefault(),
                NumberOfGeneratedTeams = bestTeams.Count
            };

        }

        public async Task<IEnumerable<ReplaceProposition>> ReplaceInMyTeam(int teamId, int numberOfPlayersToReplace, List<OptimalisationParameter> parameters)
        {
            var gameweeks = await _fplGameweekClient.GetGameweeks();
            var currentGameweek = gameweeks.FirstOrDefault(x => x.IsCurrent);
            var gameweekRange = Enumerable.Range(1, currentGameweek.Id);

            var teamHistory = new List<FplEntryPicks>();

            foreach (var gw in gameweekRange)
            {
                var pick = await _fplEntryClient.GetPicks(teamId, gw);
                teamHistory.Add(pick);
            }

            var currnetPick = teamHistory.FirstOrDefault(x => x.EventEntryHistory.Event == currentGameweek.Id);

            var players = await GetPlayers();



            if (parameters.Any(x => x.PropertyName != nameof(FplPlayer.Id)))
            {
                parameters.Add(new OptimalisationParameter
                {
                    PropertyName = nameof(FplPlayer.Id),
                    DisplayOnly = true
                });
            }

            var optimalisation = PlayersOptimalisation(parameters.ToArray(), players);

            players = players.Where(x => x.Minutes > 0).Select(x =>
            {
                var optimalPlayer = optimalisation.Results.FirstOrDefault(o => Convert.ToInt32(o[nameof(FplPlayer.Id)]) == x.Id);
                var total = Convert.ToDouble(optimalPlayer["Total"]);
                x.Total = total;

                return x;

            }).ToList();

            var playersInTeam = players.Where(x => currnetPick.Picks.Any(p => p.PlayerId == x.Id))
                                       .Select(x =>
                                       {
                                           var optimalPlayer = optimalisation.Results.FirstOrDefault(o => Convert.ToInt32(o[nameof(FplPlayer.Id)]) == x.Id);
                                           var total = Convert.ToDouble(optimalPlayer["Total"]);

                                           var player = x.MapToBasic();
                                           player.Total = total;

                                           return player;
                                       }).ToList();

            foreach (var playerInTeam in playersInTeam)
            {
                var gameweekWitchPlayerWasBought = teamHistory.OrderByDescending(x => x.EventEntryHistory.Event).FirstOrDefault(x => x.Picks.All(p => p.PlayerId != playerInTeam.Id))?.EventEntryHistory?.Event ?? 1;
                var summary = await _fplPlayerClient.GetPlayer(playerInTeam.Id);

                playerInTeam.PurchaseCost = summary.MatchStats.FirstOrDefault(x => x.Round == gameweekWitchPlayerWasBought).Value;
                decimal diff = (playerInTeam.NowCost - playerInTeam.PurchaseCost);
                diff = (diff > 0 ? Math.Floor(diff / 2) : diff);
                playerInTeam.SellingCost = (int)(playerInTeam.NowCost - diff);
            }

            var result = new List<ReplaceProposition>();

            var combinations = Backpack.GetPermutations(playersInTeam, numberOfPlayersToReplace)
                                      .Select(x => new ReplaceProposition
                                      {
                                          Current = x.ToList()
                                      });

            result.AddRange(combinations);

            Func<double, IEnumerable<FplPlayerExtension>, int, bool> condition = (currentPlayersSumOfTotal, alternative, moneyToAvailable) => alternative.Sum(p => p.NowCost) <= moneyToAvailable && alternative.Sum(x => x.Total) > currentPlayersSumOfTotal;

            var playersNotInCurrentTeam = players.Where(x => playersInTeam.All(p => p.Id != x.Id));

            foreach (var playersCombination in result)
            {
                var availableMoney = currnetPick.EventEntryHistory.Bank + playersCombination.Current.Sum(x => x.SellingCost);
                var currentPlayersSumOfTotal = playersCombination.Current.Sum(x => x.Total);

                Dictionary<FplPlayerPosition, int> playersOnPositions = new Dictionary<FplPlayerPosition, int>();

                playersOnPositions.Add(FplPlayerPosition.Defender, playersCombination.Current.Count(x => x.Position == FplPlayerPosition.Defender));
                playersOnPositions.Add(FplPlayerPosition.Midfielder, playersCombination.Current.Count(x => x.Position == FplPlayerPosition.Midfielder));
                playersOnPositions.Add(FplPlayerPosition.Forward, playersCombination.Current.Count(x => x.Position == FplPlayerPosition.Forward));
                playersOnPositions.Add(FplPlayerPosition.Goalkeeper, playersCombination.Current.Count(x => x.Position == FplPlayerPosition.Goalkeeper));

                playersOnPositions = playersOnPositions.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);


                var firstPosition = playersOnPositions.ElementAt(0);
                var secoundPosition = playersOnPositions.ElementAt(1);
                var thirdPosition = playersOnPositions.ElementAt(2);
                var fourthPosition = playersOnPositions.ElementAt(3);

                var alternatives = new List<IEnumerable<FplPlayerExtension>>();

                foreach (var firstPositionPlayers in Backpack.GetPermutations(playersNotInCurrentTeam.Where(x => x.Position == firstPosition.Key), firstPosition.Value))
                {
                    if (secoundPosition.Value > 0)
                    {
                        foreach (var secoundPositionPlayers in Backpack.GetPermutations(playersNotInCurrentTeam.Where(x => x.Position == secoundPosition.Key), secoundPosition.Value))
                        {
                            if (thirdPosition.Value > 0)
                            {
                                foreach (var thirdPositionPlayers in Backpack.GetPermutations(playersNotInCurrentTeam.Where(x => x.Position == thirdPosition.Key), thirdPosition.Value))
                                {
                                    if (fourthPosition.Value > 0)
                                    {
                                        foreach (var fourthPositionPlayers in Backpack.GetPermutations(playersNotInCurrentTeam.Where(x => x.Position == fourthPosition.Key), fourthPosition.Value))
                                        {
                                            var alternative = firstPositionPlayers.Concat(secoundPositionPlayers).Concat(thirdPositionPlayers).Concat(fourthPositionPlayers);
                                            if (condition.Invoke(currentPlayersSumOfTotal, alternative, availableMoney))
                                            {
                                                alternatives.Add(alternative);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var alternative = firstPositionPlayers.Concat(secoundPositionPlayers).Concat(thirdPositionPlayers);
                                        if (condition.Invoke(currentPlayersSumOfTotal, alternative, availableMoney))
                                        {
                                            alternatives.Add(alternative);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var alternative = firstPositionPlayers.Concat(secoundPositionPlayers);
                                if (condition.Invoke(currentPlayersSumOfTotal, alternative, availableMoney))
                                {
                                    alternatives.Add(alternative);
                                }
                            }
                        }

                    }
                    else
                    {
                        var alternative = firstPositionPlayers;
                        if (condition.Invoke(currentPlayersSumOfTotal, alternative, availableMoney))
                        {
                            alternatives.Add(alternative);
                        }
                    }
                }

                playersCombination.Alternative = alternatives.OrderByDescending(x => x.Sum(p => p.Total)).FirstOrDefault()?.Select(x => x.MapToBasic())?.ToList();
            }

            return result;
        }
    }
}
