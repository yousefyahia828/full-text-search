using FullText.API.Database;
using FullText.API.Endpoints;
using FullText.API.Extensions;
using FullText.API.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("blogs"))
     .UseSnakeCaseNamingConvention());

builder.AddRedisDistributedCache("redis");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors();

builder.AddServiceDefaults();

var app = builder.Build();

app.ApplyMigrations();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger().UseSwaggerUI();
    app.MapDefaultEndpoints();
}

app.UseCors(cors => cors
    .WithOrigins("https://fulltext.vercel.app")
    .WithMethods("GET"));

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.MapBlogEndpoints();

app.Run();
