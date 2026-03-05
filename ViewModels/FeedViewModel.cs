using AuraNews.Models;
using AuraNews.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuraNews.ViewModels;

public partial class FeedViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly NewsService _newsService;
    private readonly AIService _aiService;

    private List<Article> _allArticles = new();

    public ObservableCollection<Article> VisibleArticles { get; } = new();

    private bool _isFirstLoad = true;
    private int _lastSourceCount = -1;

    // --- X-STYLE FEED PROPERTIES ---
    private List<Article> _pendingNewArticles = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool hasNewArticles;

    [ObservableProperty]
    private string newArticlesText = "↑ Neue Artikel";

    // --- KATEGORIEN PROPERTIES ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCategorySettingsVisible))]
    private string selectedCategory = "Allgemein";

    public bool IsCategorySettingsVisible => SelectedCategory != "Allgemein";

    [ObservableProperty]
    private bool isIncludedInMainFeed = true;

    public ObservableCollection<string> Categories { get; } = new() { "Allgemein" };

    public FeedViewModel(DatabaseService databaseService, NewsService newsService, AIService aiService)
    {
        _databaseService = databaseService;
        _newsService = newsService;
        _aiService = aiService;

        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            await LoadFeedAsync();
        }
    }

    [RelayCommand]
    public async Task LoadFeedAsync()
    {
        if (IsBusy && _isFirstLoad) return;

        if (_isFirstLoad)
        {
            IsBusy = true;
            // 1. Lade SOFORT die lokalen Artikel ohne Wartezeit
            var localArticles = await _databaseService.GetSavedArticlesAsync();
            if (localArticles != null && localArticles.Any())
            {
                _allArticles = localArticles.ToList();
                await ApplyFilterAsync();
                UpdateCategories();
            }
            _isFirstLoad = false;
            IsBusy = false;
        }
        else
        {
            IsRefreshing = true;
        }

        // 2. Suche im Hintergrund lautlos nach neuen Artikeln
        _ = Task.Run(async () =>
        {
            try
            {
                var sources = await _databaseService.GetFollowedSourcesAsync();
                if (sources.Count == 0) return;

                var newArticlesFetched = new List<Article>();

                foreach (var source in sources)
                {
                    // HIER WAR DER FEHLER: Wir nutzen jetzt die richtige Methode!
                    var rawArticles = await _newsService.GetArticlesFromSourceAsync(source);
                    var blockedTopics = await _databaseService.GetBlockedTopicsForSourceAsync(source.Id);

                    foreach (var article in rawArticles.Take(5)) // Hole die neusten 5
                    {
                        // 1. Prüfen ob OriginalUrl schon bekannt ist (war schon da)
                        if (_allArticles.Any(a => a.OriginalUrl == article.OriginalUrl))
                            continue;

                        // 2. NEU: Prüfen, ob eine inhaltlich fast identische Meldung schon in den ALTEN Artikeln ist
                        if (_allArticles.Any(a => AreTitlesSimilar(a.Title, article.Title)))
                            continue;

                        // 3. NEU: Prüfen, ob wir in DIESEM Ladevorgang schon eine ähnliche Meldung aufgenommen haben
                        if (newArticlesFetched.Any(a => AreTitlesSimilar(a.Title, article.Title)))
                            continue;

                        article.Topic = !string.IsNullOrWhiteSpace(source.Category) ? source.Category : "Allgemein";

                        if (blockedTopics.Contains(article.Topic, StringComparer.OrdinalIgnoreCase))
                            continue;

                        newArticlesFetched.Add(article);
                    }
                }

                if (newArticlesFetched.Any())
                {
                    // Neue Artikel speichern, aber noch nicht anzeigen!
                    await _databaseService.SaveArticlesAsync(newArticlesFetched);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (VisibleArticles.Any())
                        {
                            _pendingNewArticles.InsertRange(0, newArticlesFetched);
                            NewArticlesText = $"↑ {_pendingNewArticles.Count} neue Nachrichten";
                            HasNewArticles = true;
                        }
                        else
                        {
                            // Wenn die App komplett leer war, sofort anzeigen
                            _ = ShowNewArticlesCommand.ExecuteAsync(null);
                        }
                    });
                }
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() => IsRefreshing = false);
            }
        });
    }

    [RelayCommand]
    public async Task ShowNewArticlesAsync()
    {
        HasNewArticles = false;

        var localArticles = await _databaseService.GetSavedArticlesAsync();
        if (localArticles != null && localArticles.Any())
        {
            _allArticles = localArticles.ToList();
            await ApplyFilterAsync();
            UpdateCategories();
            _pendingNewArticles.Clear();
        }
    }

    private void UpdateCategories()
    {
        var currentSelected = SelectedCategory;
        Categories.Clear();
        Categories.Add("Allgemein");

        var uniqueTopics = _allArticles.Select(a => a.Topic).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        foreach (var topic in uniqueTopics)
        {
            if (topic != "Allgemein") Categories.Add(topic);
        }

        if (!Categories.Contains(currentSelected)) SelectedCategory = "Allgemein";
    }

    [RelayCommand]
    public async Task SelectCategoryAsync(string category)
    {
        if (SelectedCategory == category) return;
        SelectedCategory = category;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        var isolated = await _databaseService.GetIsolatedCategoriesAsync();
        IsIncludedInMainFeed = !isolated.Contains(SelectedCategory, StringComparer.OrdinalIgnoreCase);

        await ApplyFilterAsync();
    }

    async partial void OnIsIncludedInMainFeedChanged(bool value)
    {
        if (!IsCategorySettingsVisible) return;
        await _databaseService.SetCategoryIsolationAsync(SelectedCategory, !value);
    }

    private async Task ApplyFilterAsync()
    {
        var isolatedCategories = await _databaseService.GetIsolatedCategoriesAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            VisibleArticles.Clear();

            foreach (var article in _allArticles)
            {
                if (SelectedCategory == "Allgemein")
                {
                    if (!isolatedCategories.Contains(article.Topic, StringComparer.OrdinalIgnoreCase))
                        VisibleArticles.Add(article);
                }
                else
                {
                    if (article.Topic.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                        VisibleArticles.Add(article);
                }
            }
        });
    }

    [RelayCommand]
    public async Task BlockTopicAsync(Article article)
    {
        if (article == null) return;

        await _databaseService.BlockTopicAsync(article.SourceId, article.Topic);

        var articlesToRemove = _allArticles.Where(a => a.SourceId == article.SourceId && a.Topic == article.Topic).ToList();
        foreach (var a in articlesToRemove)
        {
            _allArticles.Remove(a);
            VisibleArticles.Remove(a);
        }
    }

    [RelayCommand]
    public async Task OpenArticleAsync(Article article)
    {
        if (article == null || string.IsNullOrEmpty(article.OriginalUrl)) return;

        try
        {
            await Shell.Current.GoToAsync($"{nameof(Views.ReaderPage)}?Url={System.Uri.EscapeDataString(article.OriginalUrl)}");
            await _databaseService.SaveLastReadArticleUrlToCloudAsync(article.OriginalUrl);
        }
        catch { }
    }

    public async Task CheckAndReloadIfNeededAsync()
    {
        var sources = await _databaseService.GetFollowedSourcesAsync();

        if (_lastSourceCount != -1 && sources.Count != _lastSourceCount)
        {
            _lastSourceCount = sources.Count;
            _isFirstLoad = true;
            _allArticles.Clear();
            VisibleArticles.Clear();
            await LoadFeedAsync();
        }
        else if (_lastSourceCount == -1)
        {
            _lastSourceCount = sources.Count;
            await LoadFeedAsync();
        }
    }

    private bool AreTitlesSimilar(string title1, string title2)
    {
        if (string.IsNullOrWhiteSpace(title1) || string.IsNullOrWhiteSpace(title2)) return false;
        
        // Einfache, schnelle Methode: Prüfen, wie viele signifikante Wörter übereinstimmen
        var words1 = title1.ToLower().Split(new[] { ' ', '-', ':', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Where(w => w.Length > 3).ToHashSet();
        var words2 = title2.ToLower().Split(new[] { ' ', '-', ':', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Where(w => w.Length > 3).ToHashSet();

        if (words1.Count == 0 || words2.Count == 0) return false;

        int commonWords = words1.Intersect(words2).Count();
        double similarity = (double)commonWords / Math.Min(words1.Count, words2.Count);

        // Wenn 70% der wichtigen Wörter gleich sind, ist es vermutlich dieselbe Meldung
        return similarity > 0.7; 
    }
}