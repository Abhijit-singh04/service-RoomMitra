using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Application.Abstractions.Location;
using RoomMitra.Application.Abstractions.Notifications;
using RoomMitra.Application.Abstractions.Repositories;
using RoomMitra.Application.Abstractions.Storage;
using RoomMitra.Application.Abstractions.Time;
using RoomMitra.Infrastructure.Auth;
using RoomMitra.Infrastructure.Identity;
using RoomMitra.Infrastructure.Location;
using RoomMitra.Infrastructure.Notifications;
using RoomMitra.Infrastructure.Options;
using RoomMitra.Infrastructure.Persistence;
using RoomMitra.Infrastructure.Repositories;
using RoomMitra.Infrastructure.Storage;
using RoomMitra.Infrastructure.Time;

namespace RoomMitra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AzureBlobOptions>(configuration.GetSection(AzureBlobOptions.SectionName));
        services.Configure<AzureAdB2COptions>(configuration.GetSection(AzureAdB2COptions.SectionName));
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));

        services.AddDbContext<RoomMitraDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.EnableSensitiveDataLogging(); // Enable to see parameter values
            options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
        });

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<RoomMitraDbContext>()
            .AddSignInManager<SignInManager<AppUser>>();

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, IdentityAuthService>();
        services.AddScoped<IOtpRequestRepository, EfOtpRequestRepository>();
        services.AddScoped<ISmsSender, ConsoleSmsSender>();

        // Firebase Phone Auth (independent from Google/Gmail/Azure AD flows)
        services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();
        services.AddScoped<IFirebasePhoneAuthService, FirebasePhoneAuthService>();

        services.AddScoped<IFlatListingRepository, EfFlatListingRepository>();
        services.AddScoped<IBlobStorage, AzureBlobStorage>();

        services.AddSingleton<IClock, SystemClock>();
        
        // Location services - Azure Maps integration
        services.Configure<AzureMapsOptions>(configuration.GetSection(AzureMapsOptions.SectionName));
        services.AddSingleton<ILocationCache, InMemoryLocationCache>();
        services.AddHttpClient<ILocationService, AzureMapsLocationService>();
        services.AddScoped<IGeoSearchService, GeoSearchService>();

        return services;
    }
}
