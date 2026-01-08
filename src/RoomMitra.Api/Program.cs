using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RoomMitra.Application;
using RoomMitra.Application.Abstractions.Security;
using RoomMitra.Infrastructure;
using RoomMitra.Infrastructure.Options;
using RoomMitra.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file in development
if (builder.Environment.IsDevelopment())
{
    var envPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
    if (File.Exists(envPath))
    {
        foreach (var line in File.ReadAllLines(envPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
            }
        }
    }
}

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// When running behind a reverse proxy (Azure App Service), respect X-Forwarded-* headers.
// This ensures Request.IsHttps reflects the original client scheme, which is critical for Secure cookies.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Azure front-ends are not in KnownNetworks/KnownProxies; clear to allow forwarding.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "RoomMitra API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Azure AD B2C JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'"
    };

    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Configure Azure AD B2C Authentication
var azureAdB2C = builder.Configuration.GetSection(AzureAdB2COptions.SectionName).Get<AzureAdB2COptions>();

const string PolicyScheme = "Bearer";
const string AzureScheme = "AzureB2C";
const string LocalScheme = "LocalJwt";

static string? TryReadIssuer(string token)
{
    try
    {
        // Parse without validating (we only use this to select a scheme)
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt?.Issuer;
    }
    catch
    {
        return null;
    }
}

// Always allow local JWTs (used by email/password, phone OTP, and external sync)
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var signingKeyBytes = Encoding.UTF8.GetBytes(jwt.SigningKey);
var signingKey = new SymmetricSecurityKey(signingKeyBytes)
{
    KeyId = string.Empty // Match the empty KeyId from token generation
};

var hasAzureConfig = azureAdB2C is not null && !string.IsNullOrEmpty(azureAdB2C.TenantName);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = PolicyScheme;
        options.DefaultChallengeScheme = PolicyScheme;
    })
    .AddPolicyScheme(PolicyScheme, "Select JWT scheme", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return LocalScheme;
            }

            var token = authHeader["Bearer ".Length..].Trim();
            var iss = TryReadIssuer(token);
            if (hasAzureConfig && !string.IsNullOrWhiteSpace(iss))
            {
                if (iss.Contains("b2clogin.com", StringComparison.OrdinalIgnoreCase) ||
                    iss.Contains("ciamlogin.com", StringComparison.OrdinalIgnoreCase) ||
                    iss.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase))
                {
                    return AzureScheme;
                }
            }

            return LocalScheme;
        };
    })
    .AddJwtBearer(LocalScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            TryAllIssuerSigningKeys = true
        };
        options.Events = new JwtBearerEvents();
    });

if (hasAzureConfig)
{
    // Log the Azure config for debugging
    var metadataAddress = azureAdB2C!.GetMetadataAddress();
    var expectedIssuer = azureAdB2C.GetIssuer();
    Console.WriteLine($"[Azure Auth] MetadataAddress: {metadataAddress}");
    Console.WriteLine($"[Azure Auth] Expected Issuer: {expectedIssuer}");
    Console.WriteLine($"[Azure Auth] ClientId (Audience): {azureAdB2C.ClientId}");

    builder.Services.AddAuthentication()
        .AddJwtBearer(AzureScheme, options =>
        {
            // Use the metadata endpoint for automatic key discovery
            options.MetadataAddress = metadataAddress;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                // Allow both with and without trailing slash
                ValidIssuers = new[]
                {
                    expectedIssuer,
                    expectedIssuer.TrimEnd('/'),
                    expectedIssuer.TrimEnd('/') + "/"
                },
                ValidateAudience = true,
                ValidAudience = azureAdB2C.ClientId,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                NameClaimType = "name",
                RoleClaimType = "roles"
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("Azure authentication failed: {Error}", context.Exception.Message);
                    Console.WriteLine($"[Azure Auth FAILED] {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Console.WriteLine($"[Azure Auth SUCCESS] Token validated for: {context.Principal?.Identity?.Name}");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("Authentication challenge issued. Error: {Error}, Description: {Description}",
                        context.Error, context.ErrorDescription);
                    return Task.CompletedTask;
                }
            };
        });
}

builder.Services.AddAuthorization(options =>
{
    // Define authorization policies for role-based access control
    options.AddPolicy("RequireAuthenticated", policy =>
        policy.RequireAuthenticatedUser());
    
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin", "Administrator"));
    
    options.AddPolicy("RequireUser", policy =>
        policy.RequireRole("User", "Admin", "Administrator"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        var configuredOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?.Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        var localDevOrigins = new[]
        {
            "http://localhost:3000",
            "https://localhost:3000",
            "http://127.0.0.1:3000",
            "https://127.0.0.1:3000",
            "http://localhost:3001",  // Allow Swagger UI
            "https://localhost:3001",
            "http://127.0.0.1:3001",
            "https://127.0.0.1:3001"
        };

        var allowedOrigins = builder.Environment.IsDevelopment()
            ? configuredOrigins.Concat(localDevOrigins).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : configuredOrigins;

        if (allowedOrigins.Length == 0)
        {
            // Safe default: no cross-origin access.
            // (In development we always include localhost; in production you should set Cors:AllowedOrigins.)
            policy.SetIsOriginAllowed(_ => false);
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
        }

        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for cookies
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("frontend");
}
else
{
    app.UseHttpsRedirection();
    app.UseCors("frontend");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        var urls = (addressesFeature?.Addresses?.Any() == true) ? addressesFeature!.Addresses : app.Urls;

        if (urls is not null && urls.Any())
        {
            foreach (var url in urls)
            {
                app.Logger.LogInformation("Server is running at {Url}", url);
                Console.WriteLine($"Server is running at {url}");
            }
        }
        else
        {
            app.Logger.LogInformation("Server started. No URLs available from features.");
            Console.WriteLine("Server started. No URLs available from features.");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Server started, but failed to resolve bound URLs.");
        Console.WriteLine($"Server started. Failed to resolve URLs: {ex.Message}");
    }
});

app.Run();
