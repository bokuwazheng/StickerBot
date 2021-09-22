using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using JournalApiClient.Handlers;
using JournalApiClient.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StickerBot.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Telegram.Bot;

namespace StickerBot
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            BotOptions botOptions = Configuration.Get<BotOptions>();

            services
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "StickerBot", Version = "v1" });
                });

            services
                .AddHttpClient<JournalApiClientService>((s, h) =>
                {
                    h.DefaultRequestVersion = HttpVersion.Version20;
                    h.BaseAddress = new(botOptions.ApiBaseAddress);
                    h.Timeout = TimeSpan.FromMinutes(botOptions.ApiTimeout);
                })
                .AddHttpMessageHandler<ClientHttpMessageHandler>();

            services
                .AddTransient<ClientHttpMessageHandler>()
                .AddTransient<IGraphQLClient, GraphQLHttpClient>(s =>
                {
                    IHttpClientFactory factory = s.GetRequiredService<IHttpClientFactory>();
                    HttpClient httpClient = factory.CreateClient(nameof(JournalApiClientService));

                    GraphQLHttpClientOptions options = new() { EndPoint = new($"{ botOptions.ApiBaseAddress }/graphql") };

                    NewtonsoftJsonSerializer serializer = new();
                    serializer.JsonSerializerSettings.ContractResolver = new DefaultContractResolver()
                    { 
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };
                    serializer.JsonSerializerSettings.Formatting = Formatting.Indented;

                    return new(options, serializer, httpClient);
                })
                .AddTransient<ITelegramBotClient>(s =>
                {
                    TelegramBotClient client = new(botOptions.BotToken);

                    // In Development use: ngrok http https://localhost:5001
                    // Set webhook: https://api.telegram.org/bot<bot_token>/setWebhook?url=<https_link_provided_by_ngrok>
                    if (_env.IsProduction())
                        client.SetWebhookAsync(botOptions.WebhookUrl);

                    return client;
                })
                .AddTransient<IJournalApiClient, JournalApiClientService>()
                .AddControllers()
                .AddNewtonsoftJson()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });
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
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
