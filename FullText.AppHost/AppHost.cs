using FullText.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("db")
    .WithImage("postgres:18.6-alpine")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("postgres");

builder.AddProject<Projects.FullText>("fulltext")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithSwaggerUI();

builder.Build().Run();
