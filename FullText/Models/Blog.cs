namespace FullText.Models;

public sealed class Blog
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Excerpt { get; init; }
    public required string Content { get; init; }
    public required DateTime DuoDate { get; init; }
}
