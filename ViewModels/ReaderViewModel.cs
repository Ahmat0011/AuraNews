using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System;

namespace AuraNews.ViewModels;

[QueryProperty(nameof(ArticleUrl), "Url")]
public partial class ReaderViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    private string _articleUrl = string.Empty;
    public string ArticleUrl
    {
        get => _articleUrl;
        set
        {
            _articleUrl = value;
            if (!string.IsNullOrEmpty(value)) _ = LoadArticleAsync(value);
        }
    }

    private readonly AuraNews.Services.DatabaseService _databaseService;

    public ReaderViewModel(AuraNews.Services.DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    private async Task LoadArticleAsync(string url)
    {
        IsLoading = true;
        try 
        {
            var decodedUrl = System.Uri.UnescapeDataString(url);
            var reader = new SmartReader.Reader(decodedUrl);
            var article = await reader.GetArticleAsync();
            
            // Wenn der Artikel lesbar ist UND mehr als 300 Zeichen echten Text hat (kein Paywall-Stummel)
            if (article != null && article.IsReadable && article.TextContent?.Length > 300) 
            {
                Title = article.Title;
                Content = article.TextContent; // Nur der reine Text!
            } 
            else 
            {
                Title = "Versteckte Paywall erkannt";
                Content = "Dieser Artikel (z.B. von 'Welt') ist leider kostenpflichtig oder nicht lesbar.\n\nDie App hat diesen Artikel soeben automatisch aus deinem Feed gelöscht, damit er dich nicht weiter stört!";
                
                // Hier wird der Müll sofort aus der DB geworfen!
                await _databaseService.DeleteArticleByUrlAsync(decodedUrl);
            }
        } 
        catch (Exception) 
        {
            Content = "Fehler beim Laden des Artikels.";
        } 
        finally 
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task OpenOriginalAsync()
    {
        if (!string.IsNullOrEmpty(ArticleUrl))
        {
            var decodedUrl = System.Uri.UnescapeDataString(ArticleUrl);
            await Microsoft.Maui.ApplicationModel.Browser.Default.OpenAsync(decodedUrl, Microsoft.Maui.ApplicationModel.BrowserLaunchMode.SystemPreferred);
        }
    }
}
