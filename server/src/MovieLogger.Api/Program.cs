using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using MovieLogger.Api.Security;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MovieLogger.DAL.Extensions;
using MovieLogger.Service.Extensions;
using MovieLogger.Service.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT returned from /api/auth/login, e.g. \"eyJhbGciOi...\""
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddMovieLoggerService();
builder.Services.AddMovieLoggerDal(builder.Configuration);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<RegistrationSettings>(builder.Configuration.GetSection(RegistrationSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing Jwt configuration section.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured. For local development, set it with " +
        "'dotnet user-secrets set \"Jwt:Key\" \"<a long random string>\"' from server/src/MovieLogger.Api. " +
        "Outside Development, supply it through the Jwt__Key environment variable.");
}

// HMAC-SHA256 signing needs a key of at least 256 bits; a shorter one would only fail at the first login.
if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < JwtSettings.MinimumKeyBytes)
{
    throw new InvalidOperationException(
        $"Jwt:Key is too short. It must be at least {JwtSettings.MinimumKeyBytes} bytes (UTF-8) for HMAC-SHA256 signing.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// In AWS the API runs behind a load balancer that terminates TLS and forwards plain HTTP. These headers
// tell the app the original client IP and scheme, so HTTPS redirection sees requests that arrived over
// HTTPS as HTTPS. The load balancer's address isn't fixed, so any proxy is trusted: the container must
// only accept traffic from the load balancer (enforced by its security group), never directly.
// ForwardedHeaders:ForwardLimit is the number of proxies in front of the API. Each one appends the address
// it received the request from to X-Forwarded-For, so with CloudFront in front of the load balancer it's 2:
// the client IP is the entry CloudFront added, and anything a client sends itself sits further left and is
// ignored.
var forwardLimit = builder.Configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1;
if (forwardLimit < 1)
{
    throw new InvalidOperationException("ForwardedHeaders:ForwardLimit must be at least 1.");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = forwardLimit;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Limits requests per client IP so a public deployment can't be flooded and passwords can't be guessed
// quickly. The IP is the one resolved from the forwarded headers above.
var rateLimits = builder.Configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();

if (rateLimits.Enabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
            }

            await context.HttpContext.Response.WriteAsJsonAsync(
                new { message = "Too many requests. Please wait a moment and try again." }, cancellationToken);
        };

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            FixedWindowPerClient(context, rateLimits.RequestsPerMinute, TimeSpan.FromMinutes(1)));
        options.AddPolicy(RateLimitSettings.LoginPolicy, context =>
            FixedWindowPerClient(context, rateLimits.LoginAttemptsPerFiveMinutes, TimeSpan.FromMinutes(5)));
        options.AddPolicy(RateLimitSettings.RegisterPolicy, context =>
            FixedWindowPerClient(context, rateLimits.RegistrationsPerHour, TimeSpan.FromHours(1)));
    });
}

static RateLimitPartition<string> FixedWindowPerClient(HttpContext context, int permitLimit, TimeSpan window) =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 });

// The frontend is hosted on a different origin from the API in deployed environments, so browsers need
// CORS permission. Origins come from Cors:AllowedOrigins (Cors__AllowedOrigins__0, __1, ... as environment
// variables). With none configured CORS stays off, as in local development where Vite proxies /api.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

foreach (var origin in corsAllowedOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        || uri.GetLeftPart(UriPartial.Authority) != origin)
    {
        throw new InvalidOperationException(
            $"Cors:AllowedOrigins contains '{origin}', which isn't an origin. Use scheme://host[:port] " +
            "with no path or trailing slash, e.g. https://app.example.com.");
    }
}

if (corsAllowedOrigins.Length > 0)
{
    // Bearer tokens are sent in the Authorization header, not cookies, so credentials aren't allowed.
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(corsAllowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Authorization", "Content-Type")));
}

builder.Services.AddHealthChecks();

var app = builder.Build();

// Must run first so everything after it sees the original scheme and client IP.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (corsAllowedOrigins.Length > 0)
{
    app.UseCors();
}

if (rateLimits.Enabled)
{
    app.UseRateLimiter();
}

app.UseAuthentication();
app.UseAuthorization();

// Liveness check for the load balancer/container orchestrator. It deliberately doesn't touch the
// database, so a database outage doesn't get healthy API instances replaced.
app.MapHealthChecks("/health").AllowAnonymous().DisableRateLimiting();

app.MapControllers();

app.Run();
