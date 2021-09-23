using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using JournalApiClient.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StickerBot.Controllers;
using StickerBot.Handlers;
using StickerBot.Options;
using StickerBot.Services;
using System;
using System.Net;
using Telegram.Bot;

namespace StickerBot
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly BotOptions _botOptions;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _botOptions = configuration.Get<BotOptions>();
            _env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (_env.IsDevelopment())
                services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "StickerBot", Version = "v1" }));

            // In Development use: ngrok http https://localhost:5001
            // Set webhook: https://api.telegram.org/bot<bot_token>/setWebhook?url=<https_link_provided_by_ngrok>
            if (_env.IsProduction())
                services.AddHostedService<WebhookService>();

            services
                .AddHttpClient(nameof(WebhookController))
                .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(_botOptions.BotToken, httpClient))
                .AddHttpMessageHandler<ClientHttpMessageHandler>();

            services
                .AddHttpClient(nameof(JournalApiClientService), httpClient =>
                {
                    httpClient.DefaultRequestVersion = HttpVersion.Version20;
                    httpClient.BaseAddress = new(_botOptions.ApiBaseAddress);
                    httpClient.Timeout = TimeSpan.FromMinutes(_botOptions.ApiTimeout);
                })
                .AddTypedClient<IGraphQLClient>(httpClient =>
                {
                    GraphQLHttpClientOptions options = new() { EndPoint = new($"{ _botOptions.ApiBaseAddress }/graphql") };

                    NewtonsoftJsonSerializer serializer = new();
                    serializer.JsonSerializerSettings.ContractResolver = new DefaultContractResolver()
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };
                    serializer.JsonSerializerSettings.Formatting = Formatting.Indented;

                    return new GraphQLHttpClient(options, serializer, httpClient);
                })
                .AddHttpMessageHandler<ClientHttpMessageHandler>();

            services
                .AddTransient<ClientHttpMessageHandler>()
                .AddTransient<IJournalApiClient, JournalApiClientService>()
                .AddControllers()
                .AddNewtonsoftJson();
        }
        
        public void Configure(IApplicationBuilder builder)
        {
            if (_env.IsDevelopment())
            {
                builder
                    .UseDeveloperExceptionPage()
                    .UseSwagger()
                    .UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "StickerBot v1"));
            }

            builder
                .UseHttpsRedirection()
                .UseRouting()
                .UseEndpoints(endpoints => 
                {
                    endpoints.MapControllerRoute(
                        name: "tgwebhook",
                        pattern: $"bot/{ _botOptions.BotToken }",
                        new { controller = "Webhook", action = "Post" });

                    endpoints.MapControllers();
                });
        }
    }
}
