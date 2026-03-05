using AuraNews.Views;

namespace AuraNews;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Hier registrieren wir alle Unter-Seiten, die nicht unten in der Tab-Leiste sind:

        // NEU: Die Login-Seite im System anmelden!
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(ReaderPage), typeof(ReaderPage));
    }
}