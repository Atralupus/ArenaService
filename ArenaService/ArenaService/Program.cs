using ArenaService;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using GraphQL.Types;
using Libplanet.Crypto;
using StackExchange.Redis;
using AddressType = ArenaService.AddressType;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var redisConnectionString = configuration["Redis:ConnectionString"]!;
var timeOut = int.Parse(configuration["Redis:TimeOut"]!);
var enableWorker = bool.Parse(configuration["Worker"]!);
var configurationOptions = new ConfigurationOptions
{
    EndPoints = { redisConnectionString },
    ConnectTimeout = timeOut,
    SyncTimeout = timeOut,
    DefaultDatabase = int.Parse(configuration["Redis:Database"]!),
};

var redis = await ConnectionMultiplexer.ConnectAsync(configurationOptions);


// Add services to the container.
builder.Services
    .AddSingleton<IConnectionMultiplexer>(_ => redis)
    .AddScoped<ISchema, StandaloneSchema>()
    .AddSingleton<RedisHealthCheck>()
    .AddSingleton<IRedisArenaParticipantsService, RedisArenaParticipantsService>()
    .AddHostedService<RedisHealthCheckService>()
    .AddGraphQL(options => options.EnableMetrics = true)
    .AddSystemTextJson()
    .AddGraphTypes(typeof(AddressType))
    .AddGraphTypes(typeof(StandaloneQuery));

if (enableWorker)
{
    builder.Services
        .AddSingleton<RpcClient>()
        .AddHostedService<RpcService>()
        .AddSingleton(new PrivateKey())
        .AddSingleton<RpcNodeHealthCheck>()
        .AddHostedService<ArenaParticipantsWorker>();
}

var healthChecksBuilder = builder.Services
    .AddHealthChecks()
    .AddCheck<RedisHealthCheck>(nameof(RedisHealthCheck));
if (enableWorker)
{
    healthChecksBuilder.AddCheck<RpcNodeHealthCheck>(nameof(RpcNodeHealthCheck));
}


var app = builder.Build();
app
    .UseRouting()
    .UseGraphQL<ISchema>()
    .UseGraphQLPlayground(new PlaygroundOptions
    {
        GraphQLEndPoint = "/graphql"
    })
    .UseEndpoints(endpoints =>
    {
        endpoints.MapHealthChecks("/ping");
    });
app.Run();
