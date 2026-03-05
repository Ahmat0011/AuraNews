using System.Globalization;

namespace AuraNews.Models;

public class RelativeTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime date)
        {
            var timeSpan = DateTime.Now - date;

            if (timeSpan <= TimeSpan.FromSeconds(60)) return "Gerade eben";
            if (timeSpan <= TimeSpan.FromMinutes(60)) return $"Vor {timeSpan.Minutes} Minuten";
            if (timeSpan <= TimeSpan.FromHours(24)) return $"Vor {timeSpan.Hours} Stunden";
            if (timeSpan <= TimeSpan.FromDays(30)) return $"Vor {timeSpan.Days} Tagen";
            
            return date.ToString("dd.MM.yyyy");
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
