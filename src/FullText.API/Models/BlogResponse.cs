namespace FullText.API.Models;

public sealed class BlogResponse
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Excerpt { get; init; }
    public required DateTime DuoDate { get; init; }
}
