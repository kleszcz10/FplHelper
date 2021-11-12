using Fpl.Api.Services;
using Fpl.Api.Tools;
using FplClient;
using FplClient.Clients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fpl.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers().AddJsonOptions(config =>
            {
                config.JsonSerializerOptions.Converters.Add(new DoubleConverter());
                config.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fpl.Api", Version = "v1" });
            });

            services.AddHttpClient<IFplEntryClient,FplEntryClient>();
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

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fpl.Api v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public class DoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && reader.GetString() == "NaN")
            {
                return double.NaN;
            }

            return reader.GetDouble(); // JsonException thrown if reader.TokenType != JsonTokenType.Number
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsNaN(value))
            {
                writer.WriteStringValue("NaN");
            }
            else if (double.IsInfinity(value))
            {
                writer.WriteStringValue("Inf");
            }
            else if (double.IsPositiveInfinity(value))
            {
                writer.WriteStringValue("Inf");
            }
            else if (double.IsNegativeInfinity(value))
            {
                writer.WriteStringValue("Inf");
            }
            else
            {
                writer.WriteNumberValue(value);
            }
        }
    }
}
