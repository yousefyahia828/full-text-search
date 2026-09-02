using FullText.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("docker-compose");

var postgres = builder.AddPostgres("db")
    .WithImage("postgres:18.6-alpine")
    .WithContainerName("full-text-postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("postgres");

var redis = builder.AddRedis("redis", 6379)
    .WithImage("redis:8")
    .WithContainerName("full-text-redis")
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.FullText_API>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WithSwaggerUI();

builder.AddDockerfile("client", "../src/FullText.Client")
    .WithContainerName("full-text-client")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(port: 8080, targetPort: 8080)
    .WithEnvironment("BACKEND_URL", api.GetEndpoint("https"))
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();