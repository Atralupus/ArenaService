using ArenaService.BackOffice.Options;
using ArenaService.Shared.Data;
using ArenaService.Shared.Jwt;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configure Basic Auth Options
builder.Services.Configure<BasicAuthOptions>(
    builder.Configuration.GetSection(BasicAuthOptions.SectionName));

builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ArenaDbContext>(options =>
    options
        .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention()
);

builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(
        $"{redisOptions.Host}:{redisOptions.Port},defaultDatabase={redisOptions.RankingDbNumber}"
    );
});

builder.Services.AddScoped<ISeasonCacheRepository, SeasonCacheRepository>();
builder.Services.AddScoped<IBattleTicketPolicyRepository, BattleTicketPolicyRepository>();
builder.Services.AddScoped<IRefreshTicketPolicyRepository, RefreshTicketPolicyRepository>();
builder.Services.AddScoped<ISeasonRepository, SeasonRepository>();
builder.Services.AddScoped<IRoundRepository, RoundRepository>();
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
builder.Services.AddScoped<IRankingSnapshotRepository, RankingSnapshotRepository>();
builder.Services.AddScoped<IAllClanRankingRepository, AllClanRankingRepository>();
builder.Services.AddScoped<IRankingRepository, RankingRepository>();
builder.Services.AddScoped<IClanRepository, ClanRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IBattleRepository, BattleRepository>();
builder.Services.AddScoped<IClanRankingRepository, ClanRankingRepository>();
builder.Services.AddScoped<IMedalRepository, MedalRepository>();

builder.Services.AddScoped<ISeasonService, SeasonService>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddScoped<ISeasonPreparationService, SeasonPreparationService>();
builder.Services.AddScoped<IRoundPreparationService, RoundPreparationService>();
builder.Services.AddScoped<ISeasonService, SeasonService>();

// Fake Key
builder.Services.AddSingleton(
    new BattleTokenGenerator(
        "LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FROEFNSUlCQ2dLQ0FRRUF1UkpPT0xhTGcrMHJyd20xNUdwMgpPWnRmMXdLeDB0dlZ1RSt0ZXFZUDZ3Zm1zTE5KZnpRcTVqYjZSVFhKU2FjRS9mN3JDQ013cnBqVUJtM2ZzTUxpCkZ1aE1ZY1IweTdYb1BHRCtHb1lXM0xYYVMwSC9RY1FMUmk5ejZKM1NyOC9UREZ6eVo0MGtlOHE2M0k1STNBWDYKSlBzUUZpNlZoNHl5MWtqZDJVTjdZazNQcjRWY3BCS1pxNnc4VFlnRWNhbWxCeXFxWWdkbjdtZjRySUtFREYvZQo3NHp1T0ZLcnE2Y0hJMGRuMGtpdTZBY3lkcWYxVUx3dDFRVHRNajdYN3h3WmRCZUNVV3AzOU16bTdDMzRjemF4CkMrZ05tQnVYZFQwT3NhMTlqZlJlRjB5cUpvNG1xMVpOMi9GY0xXZVZyYStVNEhZdXJqcTJqYjIyRWxiSDYxak8KOFFJREFRQUIKLS0tLS1FTkQgUFVCTElDIEtFWS0tLS0tCg=="
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

// Basic Authentication Handler
public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration)
        : base(options, logger, encoder, clock)
    {
        _configuration = configuration;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            Response.Headers.Add("WWW-Authenticate", "Basic");
            return AuthenticateResult.Fail("Authorization header not found.");
        }

        var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
        var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? string.Empty);
        var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
        var username = credentials[0];
        var password = credentials[1];

        var config = _configuration.GetSection(BasicAuthOptions.SectionName).Get<BasicAuthOptions>();

        if (username == config?.Username && password == config?.Password)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        Response.Headers.Add("WWW-Authenticate", "Basic");
        return AuthenticateResult.Fail("Invalid username or password");
    }
}
