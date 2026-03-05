using SQLite;
using AuraNews.Models;

namespace AuraNews.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _database = null!;

    public DatabaseService()
    {
    }

    private async Task Init()
    {
        if (_database is not null)
            return;

        // Pfad zur lokalen Datenbank auf dem Smartphone
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "AuraNews.db3");
        _database = new SQLiteAsyncConnection(dbPath);
        
        // Tabellen erstellen
        await _database.CreateTableAsync<Source>();
        await _database.CreateTableAsync<BlockedTopic>();
        await _database.CreateTableAsync<IsolatedCategory>();
        await _database.CreateTableAsync<Article>();

        // Datenbank-Staubsauger: Alte Artikel entsorgen
        await CleanupOldArticlesAsync();
    }

    public async Task CleanupOldArticlesAsync()
    {
        if (_database == null) return;
        
        try
        {
            var threeDaysAgo = DateTime.Now.AddDays(-3);
            var oldArticles = await _database.Table<Article>().Where(a => a.PublishedDate < threeDaysAgo).ToListAsync();
            foreach (var article in oldArticles)
            {
                await _database.DeleteAsync(article);
            }
        }
        catch { }
    }

    // --- Methoden für Quellen ---
    public async Task<List<Source>> GetFollowedSourcesAsync()
    {
        await Init();
        return await _database.Table<Source>().Where(s => s.IsFollowed).ToListAsync();
    }

    public async Task SaveSourceAsync(Source source)
    {
        await Init();
        if (source.Id != 0)
            await _database.UpdateAsync(source);
        else
            await _database.InsertAsync(source);

        string email = Microsoft.Maui.Storage.Preferences.Get("UserEmail", null);
        if (!string.IsNullOrEmpty(email))
        {
            try
            {
                await Plugin.CloudFirestore.CrossCloudFirestore.Current
                    .Instance
                    .Collection("Users")
                    .Document(email)
                    .Collection("Subscriptions")
                    .Document(source.Name)
                    .SetAsync(new { Name = source.Name, Url = source.Url, Category = source.Category, Language = source.Language }, true);
            }
            catch { }
        }
    }

    // --- Methoden für Blockierte Themen ---
    public async Task BlockTopicAsync(int sourceId, string topicName)
    {
        await Init();
        var blockedTopic = new BlockedTopic { SourceId = sourceId, TopicName = topicName };
        await _database.InsertAsync(blockedTopic);
        await _database.ExecuteAsync("DELETE FROM Article WHERE SourceId = ? AND Topic = ?", sourceId, topicName);
    }

    public async Task<List<string>> GetBlockedTopicsForSourceAsync(int sourceId)
    {
        await Init();
        var topics = await _database.Table<BlockedTopic>().Where(b => b.SourceId == sourceId).ToListAsync();
        return topics.Select(t => t.TopicName).ToList();
    }

    public async Task DeleteSourceAsync(Source source)
    {
        await Init();
        await _database.DeleteAsync(source);

        string email = Microsoft.Maui.Storage.Preferences.Get("UserEmail", null);
        if (!string.IsNullOrEmpty(email))
        {
            try
            {
                await Plugin.CloudFirestore.CrossCloudFirestore.Current
                    .Instance
                    .Collection("Users")
                    .Document(email)
                    .Collection("Subscriptions")
                    .Document(source.Name)
                    .DeleteAsync();
            }
            catch { }
        }
    }

    public async Task DeleteBlockedTopicAsync(string topicName)
    {
        await Init();
        var topic = await _database.Table<BlockedTopic>().Where(b => b.TopicName == topicName).FirstOrDefaultAsync();
        if (topic != null)
        {
            await _database.DeleteAsync(topic);
        }
    }

    public async Task DeleteArticleByUrlAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        await Init();
        // Wir prüfen OriginalUrl und Url zur Sicherheit
        await _database.ExecuteAsync("DELETE FROM Article WHERE OriginalUrl = ? OR Url = ?", url, url);
    }

    public async Task<List<string>> GetIsolatedCategoriesAsync()
    {
        await Init();
        var items = await _database.Table<IsolatedCategory>().ToListAsync();
        return items.Select(i => i.CategoryName).ToList();
    }

    public async Task SetCategoryIsolationAsync(string categoryName, bool isolate)
    {
        await Init();
        var existing = await _database.Table<IsolatedCategory>().Where(i => i.CategoryName == categoryName).FirstOrDefaultAsync();
        
        if (isolate && existing == null)
        {
            await _database.InsertAsync(new IsolatedCategory { CategoryName = categoryName });
        }
        else if (!isolate && existing != null)
        {
            await _database.DeleteAsync(existing);
        }
    }

    public async Task SaveArticlesAsync(IEnumerable<Article> articles)
    {
        await Init();
        foreach (var article in articles)
        {
            await _database.InsertOrReplaceAsync(article);
        }
    }

    public async Task<List<Article>> GetSavedArticlesAsync()
    {
        await Init();
        return await _database.Table<Article>().OrderByDescending(a => a.PublishedDate).ToListAsync();
    }

    // --- Cloud Sync (Plugin.CloudFirestore) ---
    public async Task SaveLastReadArticleUrlToCloudAsync(string url)
    {
        // Temporär deaktiviert, um App-Blockaden zu verhindern!
        // Die App speichert gelesene Artikel jetzt blitzschnell lokal.
        await Task.CompletedTask;
    }

    public async Task SyncToCloudAsync()
    {
        await Task.CompletedTask;
    }

}
