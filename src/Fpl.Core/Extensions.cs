using Fpl.Core.Services;
using Fpl.Core.Tools;
using FplClient;
using FplClient.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Fpl.Core
{
    public static class Extensions
    {
        public static IServiceCollection AddCore(this IServiceCollection services)
        {
            services.AddHttpClient<IFplEntryClient, FplEntryClient>();
            services.AddHttpClient<IFplEntryHistoryClient, FplEntryHistoryClient>();
            services.AddHttpClient<IFplFixtureClient, FplFixtureClient>();
            services.AddHttpClient<IFplGameweekClient, FplGameweekClient>();
            services.AddHttpClient<IFplGlobalSettingsClient, FplGlobalSettingsClient>();
            services.AddHttpClient<IFplLeagueClient, FplLeagueClient>();
            services.AddHttpClient<IFplLiveGameweekStatsClient, FplLiveGameweekStatsClient>();
            services.AddHttpClient<IFplPlayerClient, FplPlayerClient>();
            services.AddHttpClient<IFplFixtureClient, FplFixtureClient>();

            services.AddMemoryCache();

            services.AddSingleton<IFplService, FplService>();

            services.AddResultsCaching<IFplEntryClient>();
            services.AddResultsCaching<IFplEntryHistoryClient>();
            services.AddResultsCaching<IFplFixtureClient>();
            services.AddResultsCaching<IFplGameweekClient>();
            services.AddResultsCaching<IFplGlobalSettingsClient>();
            services.AddResultsCaching<IFplLeagueClient>();
            services.AddResultsCaching<IFplLiveGameweekStatsClient>();
            services.AddResultsCaching<IFplPlayerClient>();
            services.AddResultsCaching<IFplFixtureClient>();

            return services;
        }
    }
}
