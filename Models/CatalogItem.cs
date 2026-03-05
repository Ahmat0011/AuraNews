using CommunityToolkit.Mvvm.ComponentModel;

namespace AuraNews.Models;

public partial class CatalogItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string RssUrl { get; set; } = string.Empty;
    public string WebsiteDomain { get; set; } = string.Empty;
    public string IconUrl => $"https://www.google.com/s2/favicons?domain={WebsiteDomain}&sz=128";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    [NotifyPropertyChangedFor(nameof(ButtonColor))]
    [NotifyPropertyChangedFor(nameof(IsButtonEnabled))]
    private bool isSubscribed;

    // Diese Werte ändern sich automatisch im Design, sobald 'isSubscribed' true wird!
    public string ButtonText => IsSubscribed ? "✓ Abonniert" : "+ Abonnieren";
    public Color ButtonColor => IsSubscribed ? Colors.Gray : Colors.DarkCyan;
    public bool IsButtonEnabled => !IsSubscribed;
}
