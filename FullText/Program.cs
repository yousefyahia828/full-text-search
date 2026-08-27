using FullText.Database;
using FullText.Extensions;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("postgres"))
     .UseSnakeCaseNamingConvention());

builder.AddServiceDefaults();

var app = builder.Build();

app.ApplyMigrations();

app.UseSwagger().UseSwaggerUI();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

var serializerOptions = new JsonSerializerOptions()
{
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

app.MapGet("blogs", async (
    ApplicationDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var blogs = await dbContext.Blogs
        .AsNoTracking()
        .Select(b => new
        {
            b.Id,
            b.Title,
            b.Excerpt,
            b.Content,
            b.DuoDate
        })
        .ToListAsync(cancellationToken);

    blogs = [.. blogs.Select(b => b with { Content = b.Content.ReplaceLineEndings("\n") })];

    var json = JsonSerializer.Serialize(blogs, serializerOptions);

    return Results.File(Encoding.UTF8.GetBytes(json), "application/json", "blogs.json");
});

app.MapGet("blogs/search", async (
    string searchTerms,
    ApplicationDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var blogs = await dbContext.Blogs
        .AsNoTracking()
        .Where(b => EF.Property<NpgsqlTsVector>(b, "SearchVector").Matches(
            EF.Functions.WebSearchToTsQuery(searchTerms)))
        .Select(b => new
        {
            b.Title,
            b.Excerpt,
            b.DuoDate,
            Relevance = EF.Property<NpgsqlTsVector>(b, "SearchVector")
                .Rank(EF.Functions.WebSearchToTsQuery(searchTerms))
        })
        .OrderByDescending(b => b.Relevance)
        .ToListAsync(cancellationToken);

    return blogs.Select(b => b with { Relevance = (float)Math.Round(b.Relevance * 100, 2) });
});

app.Run();
