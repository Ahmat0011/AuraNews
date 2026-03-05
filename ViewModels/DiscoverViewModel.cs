using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AuraNews.Models;
using AuraNews.Services;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.Maui.ApplicationModel;

namespace AuraNews.ViewModels;

public partial class DiscoverViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;

    public ObservableCollection<Source> FollowedSources { get; } = new();
    public ObservableCollection<CatalogItem> RecommendedProviders { get; } = new();

    public DiscoverViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await LoadSourcesAsync();
        await LoadRecommendedProvidersAsync();
    }

    private async Task LoadRecommendedProvidersAsync()
    {
        RecommendedProviders.Clear();
        var allProviders = new List<CatalogItem>
        {
            new CatalogItem { Name = "Tagesschau", WebsiteDomain = "tagesschau.de", RssUrl = "https://www.tagesschau.de/xml/rss2/" },
            new CatalogItem { Name = "ZDFheute", WebsiteDomain = "zdf.de", RssUrl = "https://www.zdf.de/rss/zdf/nachrichten" },
            new CatalogItem { Name = "Deutschlandfunk", WebsiteDomain = "deutschlandfunk.de", RssUrl = "https://www.deutschlandfunk.de/nachrichten-100.rss" },
            new CatalogItem { Name = "BR24 (Bayern)", WebsiteDomain = "br.de", RssUrl = "https://www.br.de/nachrichten/meldungen.xml" },
            new CatalogItem { Name = "NDR (Nord)", WebsiteDomain = "ndr.de", RssUrl = "https://www.ndr.de/nachrichten/index-rss.xml" },
            new CatalogItem { Name = "Spiegel", WebsiteDomain = "spiegel.de", RssUrl = "https://www.spiegel.de/schlagzeilen/tops/index.rss" },
            new CatalogItem { Name = "FAZ", WebsiteDomain = "faz.net", RssUrl = "https://www.faz.net/rss/aktuell/" },
            new CatalogItem { Name = "Süddeutsche (SZ)", WebsiteDomain = "sueddeutsche.de", RssUrl = "https://rss.sueddeutsche.de/app/rss/sz/schlagzeilen" },
            new CatalogItem { Name = "Welt", WebsiteDomain = "welt.de", RssUrl = "https://www.welt.de/feeds/topnews.rss" },
            new CatalogItem { Name = "Zeit Online", WebsiteDomain = "zeit.de", RssUrl = "https://newsfeed.zeit.de/index" },
            new CatalogItem { Name = "N-TV", WebsiteDomain = "n-tv.de", RssUrl = "https://www.n-tv.de/rss" },
            new CatalogItem { Name = "Handelsblatt", WebsiteDomain = "handelsblatt.com", RssUrl = "https://www.handelsblatt.com/contentexport/feed/top-themen" },
            new CatalogItem { Name = "Stern", WebsiteDomain = "stern.de", RssUrl = "https://www.stern.de/feed/standard/alle-nachrichten/" }
        };

        var sources = await _databaseService.GetFollowedSourcesAsync();

        foreach (var provider in allProviders)
        {
            if (!sources.Any(s => s.Url == provider.RssUrl))
            {
                RecommendedProviders.Add(provider);
            }
        }
    }

    [RelayCommand]
    public async Task LoadSourcesAsync()
    {
        FollowedSources.Clear();
        var sources = await _databaseService.GetFollowedSourcesAsync();
        foreach (var source in sources)
        {
            FollowedSources.Add(source);
        }
    }

    // NEU: Diese Methode blockiert das UI nicht mehr!
    [RelayCommand]
    public void SubscribeToProvider(CatalogItem item)
    {
        if (item == null) return;

        // 1. UI SOFORT aktualisieren, keine Wartezeit!
        RecommendedProviders.Remove(item);

        var source = new Source
        {
            Name = item.Name,
            Url = item.RssUrl,
            IsFollowed = true,
            Language = "de",
            Category = "Allgemein"
        };
        FollowedSources.Add(source);

        // 2. Google Cloud Upload im Hintergrund erledigen
        Task.Run(async () =>
        {
            try
            {
                await _databaseService.SaveSourceAsync(source);
            }
            catch { }
        });
    }

    // NEU: Auch das Löschen eines Abos ist jetzt sofort!
    [RelayCommand]
    public void UnfollowSource(Source source)
    {
        if (source == null) return;

        // 1. UI SOFORT aktualisieren
        FollowedSources.Remove(source);

        // 2. In der Cloud löschen im Hintergrund
        Task.Run(async () =>
        {
            try
            {
                await _databaseService.DeleteSourceAsync(source);

                // Wir laden die Empfehlungen neu, damit die gelöschte Zeitung wieder oben auftaucht
                MainThread.BeginInvokeOnMainThread(async () => {
                    await LoadRecommendedProvidersAsync();
                });
            }
            catch { }
        });
    }
}