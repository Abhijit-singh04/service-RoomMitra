using Microsoft.Extensions.DependencyInjection;
using RoomMitra.Application.Abstractions.Listings;
using RoomMitra.Application.Services;

namespace RoomMitra.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IListingsService, ListingsService>();
        return services;
    }
}
