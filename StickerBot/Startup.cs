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
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Telegram.Bot;

namespace StickerBot
{
    public class Startup
    {
        private IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            static string GetEV(string key) => Environment.GetEnvironmentVariable(key);

            Uri baseAddress = new(GetEV("ApiBaseAddress"));
            TimeSpan timeout = TimeSpan.FromMinutes(Convert.ToInt32(Environment.GetEnvironmentVariable("ApiTimeout")));
            string token = GetEV("BotToken");
            string webhook = GetEV("WebhookUrl");

            services
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "StickerBot", Version = "v1" });
                });

            services
                .AddHttpClient<JournalApiClient.Services.JournalApiClient>((s, h) =>
                {
                    h.DefaultRequestVersion = HttpVersion.Version20;
                    h.BaseAddress = baseAddress;
                    h.Timeout = timeout;
                })
                .AddHttpMessageHandler<ClientHttpMessageHandler>();

            services
                .AddTransient<ClientHttpMessageHandler>()
                .AddTransient<IGraphQLClient, GraphQLHttpClient>(s =>
                {
                    IHttpClientFactory factory = s.GetRequiredService<IHttpClientFactory>();
                    HttpClient httpClient = factory.CreateClient(nameof(JournalApiClient.Services.JournalApiClient));

                    GraphQLHttpClientOptions options = new()
                    {
                        EndPoint = baseAddress,
                        //HttpMessageHandler = s.GetRequiredService<ClientHttpMessageHandler>()
                    };

                    DefaultContractResolver contractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };

                    NewtonsoftJsonSerializer serializer = new();
                    serializer.JsonSerializerSettings.ContractResolver = contractResolver;
                    serializer.JsonSerializerSettings.Formatting = Formatting.Indented;

                    return new(options, serializer, httpClient);
                })
                .AddTransient<ITelegramBotClient>(s =>
                {
                    TelegramBotClient client = new(token);
                    client.SetWebhookAsync(webhook);
                    return client;
                })
                .AddTransient<IJournalApiClient, JournalApiClient.Services.JournalApiClient>()
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
