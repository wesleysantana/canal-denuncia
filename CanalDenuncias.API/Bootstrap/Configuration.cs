using CanalDenuncias.Infra.Data.Configurations;
using CanalDenuncias.Infra.Data.Context;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CanalDenuncias.API.IoC;

public class Configuration
{
    public static void Register(IServiceCollection service, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Udms");

        service.AddDbContext<DataContext>(options => options.UseOracle(connectionString));

        // AppSettings (PathFileStorage)
        service.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // Logging
        service.AddLogging();

        // Keycloak Authentication & Authorization
        service.AddKeycloakWebApiAuthentication(configuration.GetSection("KeyCloak"));
        service.AddKeycloakAuthorization(configuration).AddAuthorizationServer(configuration);
    }
}