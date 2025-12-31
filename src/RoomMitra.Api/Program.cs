using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

if (azureAdB2C is not null && !string.IsNullOrEmpty(azureAdB2C.TenantName))
{
    // Azure AD B2C JWT Bearer Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Use the metadata endpoint for automatic key discovery
            options.MetadataAddress = azureAdB2C.GetMetadataAddress();
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Validate the token issuer
                ValidateIssuer = true,
                ValidIssuer = azureAdB2C.GetIssuer(),
                
                // Validate the token audience (your API's client ID)
                ValidateAudience = true,
                ValidAudience = azureAdB2C.ClientId,
                
                // Azure AD B2C uses asymmetric keys (RS256), not symmetric
                // The signing keys are automatically retrieved from the JWKS endpoint
                ValidateIssuerSigningKey = true,
                
                // Validate token lifetime
                ValidateLifetime = true,
                
                // Allow a small clock skew for distributed systems
                ClockSkew = TimeSpan.FromMinutes(2),
                
                // Azure AD B2C specific claim mappings
                NameClaimType = "name",
                RoleClaimType = "roles"
            };

            // Configure events for debugging and logging
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("Authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
                    logger.LogDebug("Token validated. Claims: {Claims}", string.Join(", ", claims ?? Array.Empty<string>()));
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
else
{
    // Fallback to local JWT authentication (for development/testing)
    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
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
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:3001",  // Allow Swagger UI
                "https://localhost:3001"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials() // Required for cookies
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // In development, allow all origins for easier testing
    app.UseCors(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
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
