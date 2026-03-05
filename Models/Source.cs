using SQLite;

namespace AuraNews.Models;

public class Source
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Language { get; set; } = "de"; // Standardmäßig Deutsch
    public string Category { get; set; } = string.Empty;
    public bool IsFollowed { get; set; }
}
