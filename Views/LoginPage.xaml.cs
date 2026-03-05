using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using System;
using Firebase.Auth;
using Firebase.Auth.Providers;
using System.Threading.Tasks;
using AuraNews.ViewModels;

namespace AuraNews.Views;

public partial class LoginPage : ContentPage
{
    // Dein API-Schlüssel
    private const string FirebaseApiKey = "AIzaSyD9iaIijS2MDckfu4Cmm1pucJntikgntaY";

    // NEU: Die geforderte Auth Domain (aus deiner Projekt-ID generiert)
    private const string FirebaseAuthDomain = "gen-lang-client-0408645008.firebaseapp.com";

    private FirebaseAuthClient _authClient;

    public LoginPage()
    {
        InitializeComponent();

        var config = new FirebaseAuthConfig
        {
            ApiKey = FirebaseApiKey,
            AuthDomain = FirebaseAuthDomain, // <-- DAS WAR DER FEHLENDE SCHLÜSSEL ZUM GLÜCK!
            Providers = new FirebaseAuthProvider[] { new EmailProvider() }
        };
        _authClient = new FirebaseAuthClient(config);
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        string email = EmailEntry.Text ?? "";
        string password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await Shell.Current.DisplayAlert("Halt", "Bitte E-Mail und Passwort eingeben!", "OK");
            return;
        }

        try
        {
            var userCredential = await _authClient.SignInWithEmailAndPasswordAsync(email, password);

            await MainThread.InvokeOnMainThreadAsync(async () => {
                Preferences.Set("UserEmail", email);

                // 2. Abos beim Login herunterladen
                var settingsViewModel = Application.Current?.Handler?.MauiContext?.Services.GetService<SettingsViewModel>();
                if (settingsViewModel != null)
                {
                    await settingsViewModel.SyncSubscriptionsFromCloudAsync();
                }

                // 3. UI-Aktualisierung im Feed erzwingen
                var feedViewModel = Application.Current?.Handler?.MauiContext?.Services.GetService<FeedViewModel>();
                if (feedViewModel != null)
                {
                    await feedViewModel.CheckAndReloadIfNeededAsync();
                }

                await Shell.Current.DisplayAlert("Erfolg", "Du bist erfolgreich eingeloggt!", "OK");
                await Shell.Current.Navigation.PopAsync();
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () => {
                await Shell.Current.DisplayAlert("Fehler beim Login", ex.Message, "OK");
            });
        }
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        string email = EmailEntry.Text ?? "";
        string password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await Shell.Current.DisplayAlert("Halt", "Bitte E-Mail und Passwort eingeben!", "OK");
            return;
        }

        try
        {
            var userCredential = await _authClient.CreateUserWithEmailAndPasswordAsync(email, password);

            await MainThread.InvokeOnMainThreadAsync(async () => {
                Preferences.Set("UserEmail", email);
                
                // 2. Abos beim Registrieren initialisieren/herunterladen (falls Account existierte)
                var settingsViewModel = Application.Current?.Handler?.MauiContext?.Services.GetService<SettingsViewModel>();
                if (settingsViewModel != null)
                {
                    await settingsViewModel.SyncSubscriptionsFromCloudAsync();
                }

                // 3. UI-Aktualisierung im Feed erzwingen
                var feedViewModel = Application.Current?.Handler?.MauiContext?.Services.GetService<FeedViewModel>();
                if (feedViewModel != null)
                {
                    await feedViewModel.CheckAndReloadIfNeededAsync();
                }

                await Shell.Current.DisplayAlert("Erfolg", "Konto erfolgreich erstellt!", "OK");
                await Shell.Current.Navigation.PopAsync();
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () => {
                await Shell.Current.DisplayAlert("Fehler beim Erstellen", ex.Message, "OK");
            });
        }
    }
}