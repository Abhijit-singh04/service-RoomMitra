using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Application.Abstractions.Repositories;
using RoomMitra.Application.Abstractions.Storage;
using RoomMitra.Application.Abstractions.Time;
using RoomMitra.Infrastructure.Auth;
using RoomMitra.Infrastructure.Identity;
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
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AzureBlobOptions>(configuration.GetSection(AzureBlobOptions.SectionName));

        services.AddDbContext<RoomMitraDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
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

        services.AddScoped<IFlatListingRepository, EfFlatListingRepository>();
        services.AddScoped<IBlobStorage, AzureBlobStorage>();

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
