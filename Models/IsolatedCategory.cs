using SQLite;

namespace AuraNews.Models;

public class IsolatedCategory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
