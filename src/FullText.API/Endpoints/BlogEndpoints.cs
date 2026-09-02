using FullText.API.Database;
using FullText.API.Extensions;
using FullText.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NpgsqlTypes;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace FullText.API.Endpoints;

public static class BlogEndpoints
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void MapBlogEndpoints(this IEndpointRouteBuilder app)
    {
        // Download
        app.MapGet("blogs/download", async (
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
                .OrderByDescending(b => b.DuoDate)
                .ToListAsync(cancellationToken);

            blogs = [.. blogs.Select(b => b with { Content = b.Content.ReplaceLineEndings("\n") })];

            var json = JsonSerializer.Serialize(blogs, _serializerOptions);

            return Results.File(Encoding.UTF8.GetBytes(json), "application/json", "blogs.json");
        });

        // Get
        app.MapGet("blogs", async (
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5) =>
        {
            IQueryable<BlogResponse> blogsQuery = dbContext.Blogs
                .AsNoTracking()
                .Select(b => new BlogResponse
                {
                    Id = b.Id,
                    Title = b.Title,
                    Excerpt = b.Excerpt,
                    DuoDate = b.DuoDate
                })
                .OrderByDescending(b => b.DuoDate);


            return await PagedResult<BlogResponse>.CreateAsync(blogsQuery, page, pageSize, cancellationToken);
        });

        // Get By Id
        app.MapGet("blogs/{id:int}", async (
            int id,
            IDistributedCache cache,
            IConnectionMultiplexer redis,
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var blog = await cache.GetOrCreateAsync(redis, $"blog:{id}", async token =>
            {
                return await dbContext.Blogs
                 .AsNoTracking()
                 .Where(b => b.Id == id)
                 .Select(b => new
                 {
                     b.Id,
                     b.Title,
                     b.Excerpt,
                     b.Content,
                     b.DuoDate,
                 })
                 .FirstOrDefaultAsync(token);
            },
            cancellationToken: cancellationToken);

            return blog is not null ? Results.Ok(blog) : Results.NotFound();
        });

        // Search
        app.MapGet("blogs/search", async (
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken,
            [FromQuery] string searchTerms,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5) =>
        {
            IQueryable<SearchBlogResponse> blogsQuery = dbContext.Blogs
               .AsNoTracking()
               .Where(b => EF.Property<NpgsqlTsVector>(b, "SearchVector")
                   .Matches(EF.Functions.WebSearchToTsQuery(searchTerms)))
               .Select(b => new SearchBlogResponse()
               {
                   Id = b.Id,
                   Title = b.Title,
                   Excerpt = b.Excerpt,
                   DuoDate = b.DuoDate,
                   Relevance = EF.Property<NpgsqlTsVector>(b, "SearchVector")
                       .Rank(EF.Functions.WebSearchToTsQuery(searchTerms))
               })
               .OrderByDescending(b => b.Relevance)
               .ThenByDescending(b => b.DuoDate);

            return await PagedResult<SearchBlogResponse>.CreateAsync(
                blogsQuery,
                page,
                pageSize,
                cancellationToken);
        });
    }
}
