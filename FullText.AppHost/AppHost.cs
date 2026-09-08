using FullText.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
        .WithImage("postgres:18.6-alpine")
        .WithContainerName("full-text-postgres")
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("blogs");

var redis = builder.AddRedis("redis", 6379)
    .WithImage("redis:8")
    .WithContainerName("full-text-redis");

builder.AddProject<Projects.FullText_API>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WithSwaggerUI()
    .WithExternalHttpEndpoints();

builder.Build().Run();