using SQLite;

namespace AuraNews.Models;

public class BlockedTopic
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int SourceId { get; set; } // Verknüpfung zur Quelle
    public string TopicName { get; set; } = string.Empty; // z.B. "Sport"
}
