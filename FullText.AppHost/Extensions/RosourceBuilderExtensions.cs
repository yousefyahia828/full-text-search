using Aspire.Hosting.Postgres;
using FullText.AppHost.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace FullText.AppHost.Extensions;

internal static class ResourceBuilderExtensions
{
    public static IResourceBuilder<T> WithPgAdmin<T>(
       this IResourceBuilder<T> builder)
       where T : PostgresServerResource, IResourceWithEndpoints
    {
        IResourceBuilder<PgAdminContainerResource>? pgAdminBuilder = null;

        builder.WithPgAdmin(pgadmin =>
        {
            pgadmin
                .WithImage("dpage/pgadmin4:9.17")
                .WithContainerName("full-text-pgadmin4")
                .WithLifetime(ContainerLifetime.Persistent)
                .WithHostPort(5050);

            pgAdminBuilder = pgadmin;
        });

        pgAdminBuilder?.WithCustomCommand(
            "pg-admin-dashboard",
            "PgAdmin4 Dashboard",
            "/",
            "Database",
            endpointName: "http");

        return builder;
    }

    public static IResourceBuilder<T> WithSwaggerUI<T>(
        this IResourceBuilder<T> builder)
        where T : IResourceWithEndpoints
    {
        return builder.WithCustomCommand(
            "swagger-open-ui",
            "Swagger API Documentation",
            "swagger",
            "Document");
    }

    internal static IResourceBuilder<T> WithCustomCommand<T>(
        this IResourceBuilder<T> builder,
        string name,
        string displayName,
        string path,
        string iconName,
        string endpointName = "https")
        where T : IResourceWithEndpoints
    {
        var commandOptions = new CommandOptions
        {
            UpdateState = context => context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy
                ? ResourceCommandState.Enabled
                : ResourceCommandState.Disabled,
            IconName = iconName,
            IconVariant = IconVariant.Filled
        };

        builder.WithCommand(
            name: name,
            displayName: displayName,
            executeCommand: async _ =>
            {
                try
                {
                    var baseUrl = builder.GetEndpoint(endpointName).Url;
                    var url = $"{baseUrl}/{path}";

                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    return CommandResults.Success();
                }
                catch (Exception ex)
                {
                    return CommandResults.Failure(ex.Message);
                }
            },
            commandOptions: commandOptions);

        return builder;
    }
}