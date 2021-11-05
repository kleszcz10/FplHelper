﻿using Fpl.Api.DTO;
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
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly FplEntryClient _fplEntryClient;
        private readonly FplEntryHistoryClient _fplEntryHistoryClient;
        private readonly FplLeagueClient _fplLeagueClient;
        private readonly FplPlayerClient _fplPlayerClient;
        private readonly FplFixtureClient _fplFixtureClient;
        private readonly FplGameweekClient _fplGameweekClient;
        private readonly FplGlobalSettingsClient _fplGlobalSettingsClient;
        private readonly FplLiveGameweekStatsClient _fplLiveGameweekStatsClient;
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache _cache;

        public HomeController(FplEntryClient fplEntryClient,
                              FplEntryHistoryClient fplEntryHistoryClient,
                              FplLeagueClient fplLeagueClient,
                              FplPlayerClient fplPlayerClient,
                              FplFixtureClient fplFixtureClient,
                              FplGameweekClient fplGameweekClient,
                              FplGlobalSettingsClient fplGlobalSettingsClient,
                              FplLiveGameweekStatsClient fplLiveGameweekStatsClient,
                              ILogger<HomeController> logger,
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
            _cache = cache;
        }
        [HttpGet]
        public Task<ICollection<FplPlayer>> PlayersList() => _fplPlayerClient.GetAllPlayers();
        [HttpGet("{gameweekId}")]
        public Task<ICollection<FplFixture>> Fixtures(int gameweekId) => _fplFixtureClient.GetFixturesByGameweek(gameweekId);
        [HttpGet]
        public async Task<ICollection<FplTeam>> Teams()
        {
            var settings = await _fplGlobalSettingsClient.GetGlobalSettings();
            return settings.Teams;
        }
        [HttpPost]
        public async Task<BestTeam2Result> Optimalisation(Parameter[] parameters)
        {
            var players = await GetPlayers();

            return await PlayersOptimalisation(parameters, players);
        }
        [HttpPost("{timeout}")]
        public async Task<BestTeam> BestTeamBasedOnOptimalisation(int timeout, List<Parameter> parameters, CancellationToken cancellationToken)
        {
            if (parameters.Any(x => x.PropertyName != nameof(FplPlayer.Id)))
            {
                parameters.Add(new Parameter
                {
                    PropertyName = nameof(FplPlayer.Id),
                    DisplayOnly = true
                });
            }

            var players = await GetPlayers();

            var optimalisation = await PlayersOptimalisation(parameters.ToArray(), players);

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

                Backpack.Combine(players.Where(x => x.Position == FplPlayerPosition.Goalkeeper).ToList(), 2).AsParallel()
                                                                                                         .WithCancellation(canncellationTokenSource.Token)
                                                                                                         .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                                                                                         .ForAll(goalkeepers =>
                {
                    try
                    {
                        Backpack.Combine(players.Where(x => x.Position == FplPlayerPosition.Forward).ToList(), 3).AsParallel()
                                                                                                              .WithCancellation(canncellationTokenSource.Token)
                                                                                                              .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                                                                                              .ForAll(forwarders =>
                        {
                            try
                            {
                                Backpack.Combine(players.Where(x => x.Position == FplPlayerPosition.Midfielder).ToList(), 5).AsParallel()
                                                                                                                         .WithCancellation(canncellationTokenSource.Token)
                                                                                                                         .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                                                                                                         .ForAll(midlefielders =>
                                {
                                    try
                                    {
                                        Backpack.Combine(players.Where(x => x.Position == FplPlayerPosition.Defender).ToList(), 5).AsParallel()
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
                                    catch (Exception ex)
                                    {
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                });

                //Parallel.ForEach(Backpack.Combine(cast.Where(x => x.Position == FplPlayerPosition.Goalkeeper).ToList(), 2), pararellOptions, goalkeepers =>
                //{
                //    if (!condition.Invoke(goalkeepers)) return;
                //    try
                //    {
                //        Parallel.ForEach(Backpack.Combine(cast.Where(x => x.Position == FplPlayerPosition.Forward).ToList(), 3), pararellOptions, forwarders =>
                //        {
                //            var goalkeepersAndForwarders = goalkeepers.Concat(forwarders);
                //            if (!condition.Invoke(goalkeepersAndForwarders)) return;
                //            try
                //            {
                //                Parallel.ForEach(Backpack.Combine(cast.Where(x => x.Position == FplPlayerPosition.Midfielder).ToList(), 5), pararellOptions, midfielders =>
                //                {
                //                    var midfieldersForwardersAndGoalkeepers = goalkeepersAndForwarders.Concat(midfielders);
                //                    if (!condition.Invoke(midfieldersForwardersAndGoalkeepers)) return;
                //                    try
                //                    {
                //                        Parallel.ForEach(Backpack.Combine(cast.Where(x => x.Position == FplPlayerPosition.Defender).ToList(), 5), pararellOptions, defenders =>
                //                        {
                //                            var team = midfieldersForwardersAndGoalkeepers.Concat(defenders);

                //                            if (condition.Invoke(team))
                //                            {
                //                                _logger.LogInformation(team.Sum(p => p.TotalPoints).ToString());
                //                                bestTeams.Add(team);
                //                            }
                //                        });
                //                    }
                //                    catch (Exception ex) { _logger.LogError(ex.Message); }

                //                });
                //            }
                //            catch (Exception ex) { _logger.LogError(ex.Message); }
                //        });
                //    }
                //    catch (Exception ex) { _logger.LogError(ex.Message); }
                //});
            }
            catch (Exception ex) { _logger.LogError(ex.Message); }

            finally
            {
                canncellationTokenSource.Dispose();
                timer.Stop();
                timer.Dispose();
            }
            return new BestTeam
            {
                Team = bestTeams.OrderBy(x => x.Sum(p => p.BackpackIndex)).FirstOrDefault(),
                NumberOfGeneratedTeams = bestTeams.Count
            };

        }
        private async Task<BestTeam2Result> PlayersOptimalisation(Parameter[] parameters, IEnumerable<FplPlayerExtension> players)
        {
            var result = new BestTeam2Result();
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

        [HttpGet]
        public IEnumerable<string> GetParameters()
        {
            return typeof(FplPlayerExtension).GetProperties().Select(x => x.Name);
        }

        [NonAction]
        private async Task<IEnumerable<FplPlayerExtension>> GetPlayers()
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
    }
}
