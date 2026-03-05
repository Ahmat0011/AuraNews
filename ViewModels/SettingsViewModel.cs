using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace AuraNews.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    // Hier können wir später den echten Login-Namen und die E-Mail speichern
    [ObservableProperty]
    private string userName = "Gast";

    [ObservableProperty]
    private string userStatus = "Lokal gespeichert";

    [ObservableProperty]
    private bool isLoggedIn = false;

    private readonly Services.DatabaseService _databaseService;

    public SettingsViewModel(Services.DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task SyncSubscriptionsFromCloudAsync()
    {
        string email = Microsoft.Maui.Storage.Preferences.Get("UserEmail", null);
        if (string.IsNullOrEmpty(email)) return;

        try
        {
            var snapshot = await Plugin.CloudFirestore.CrossCloudFirestore.Current
                .Instance
                .Collection("Users")
                .Document(email)
                .Collection("Subscriptions")
                .GetAsync();

            var currentSources = await _databaseService.GetFollowedSourcesAsync();

            foreach (var document in snapshot.Documents)
            {
                var dict = document.Data;
                string name = dict.ContainsKey("Name") ? dict["Name"]?.ToString() : document.Id;
                string url = dict.ContainsKey("Url") ? dict["Url"]?.ToString() : "";
                string category = dict.ContainsKey("Category") ? dict["Category"]?.ToString() : "Allgemein";
                string language = dict.ContainsKey("Language") ? dict["Language"]?.ToString() : "de";

                if (!string.IsNullOrEmpty(url) && !currentSources.Any(s => s.Url == url))
                {
                    var source = new Models.Source
                    {
                        Name = name,
                        Url = url,
                        IsFollowed = true,
                        Language = language,
                        Category = category
                    };
                    await _databaseService.SaveSourceAsync(source);
                }
            }
        }
        catch { }
    }

    // Dieser Befehl wird ausgeführt, wenn du auf "Anmelden" klickst!
    [RelayCommand]
    public async Task OpenLoginAsync()
    {
        // Springt sicher zur neuen Login-Seite
        await Shell.Current.GoToAsync(nameof(Views.LoginPage));
    }

    public void UpdateState()
    {
        string email = Microsoft.Maui.Storage.Preferences.Get("UserEmail", null);
        if (!string.IsNullOrEmpty(email))
        {
            UserName = email;
            UserStatus = "Cloud-Sync aktiv";
            IsLoggedIn = true;
        }
        else
        {
            UserName = "Gast";
            UserStatus = "Lokal gespeichert";
            IsLoggedIn = false;
        }
    }

    [RelayCommand]
    public void Logout()
    {
        Microsoft.Maui.Storage.Preferences.Remove("UserEmail");
        UpdateState();
    }
}