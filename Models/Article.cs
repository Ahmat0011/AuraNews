using SQLite;

namespace AuraNews.Models;

public class Article
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    
    [PrimaryKey]
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty; // NEU: Name der Quelle für die Anzeige
    public string Topic { get; set; } = string.Empty; 
    public string ImageUrl { get; set; } = string.Empty;   // NEU: Für das Artikelbild (Syft-Style)
}
