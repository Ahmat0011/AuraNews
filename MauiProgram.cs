using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace AuraNews;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // #region Services
        builder.Services.AddSingleton<Services.DatabaseService>();
        builder.Services.AddSingleton<Services.NewsService>();
        // Neu hinzugefügt:
        builder.Services.AddSingleton<Services.AIService>();
        // #endregion

        // #region ViewModels
        builder.Services.AddSingleton<ViewModels.FeedViewModel>();
        builder.Services.AddSingleton<ViewModels.DiscoverViewModel>();
        builder.Services.AddSingleton<ViewModels.SettingsViewModel>();
        builder.Services.AddTransient<ViewModels.ReaderViewModel>();
        // #endregion

        // #region Views
        builder.Services.AddSingleton<Views.FeedPage>();
        builder.Services.AddSingleton<Views.DiscoverPage>();
        builder.Services.AddSingleton<Views.SettingsPage>();
        builder.Services.AddTransient<Views.ReaderPage>();
        // #endregion

        return builder.Build();
    }
}
