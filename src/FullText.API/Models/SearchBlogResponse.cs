namespace FullText.API.Models;

public sealed record SearchBlogResponse
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Excerpt { get; init; }
    public required DateTime DuoDate { get; init; }
    public required float Relevance { get; init; }
}