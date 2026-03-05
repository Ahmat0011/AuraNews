using System.Xml.Linq;
using AuraNews.Models;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;

namespace AuraNews.Services;

public class NewsService
{
    private readonly HttpClient _httpClient;

    public NewsService()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<List<Article>> GetArticlesFromSourceAsync(Source source)
    {
        var articles = new List<Article>();

        try
        {
            var responseString = await _httpClient.GetStringAsync(source.Url);
            var doc = XDocument.Parse(responseString);
            var items = doc.Descendants().Where(x => x.Name.LocalName == "item" || x.Name.LocalName == "entry").ToList();

            foreach (var item in items.Take(5))
            {
                var titleRaw = item.Elements().FirstOrDefault(x => x.Name.LocalName == "title")?.Value ?? "Kein Titel";
                var descRaw = item.Elements().FirstOrDefault(x => x.Name.LocalName == "description" || x.Name.LocalName == "summary")?.Value ?? string.Empty;
                var link = item.Elements().FirstOrDefault(x => x.Name.LocalName == "link")?.Attribute("href")?.Value 
                           ?? item.Elements().FirstOrDefault(x => x.Name.LocalName == "link")?.Value ?? string.Empty;

                var cleanTitle = WebUtility.HtmlDecode(titleRaw);
                var cleanDesc = WebUtility.HtmlDecode(Regex.Replace(descRaw, "<.*?>", string.Empty).Trim());

                // Bekannte Paywall-Marker deutscher Verlage
                var paywallMarkers = new[] { "F+", "[F+]", "Z+", "SPIEGEL+", "SZ+", "BILDplus", "WELT+", "Abo" };
                
                // Wenn ein Marker im Titel oder Text auftaucht, überspringe den Artikel komplett
                if (paywallMarkers.Any(marker => cleanTitle.Contains(marker) || cleanDesc.Contains(marker)))
                {
                    continue;
                }

                // --- 1. ECHTE BILDER SUCHEN (Open Graph Geheimwaffe) ---
                string imageUrl = string.Empty;
                
                // Versuche zuerst offizielle RSS-Bilder (Vermeidet Werbe-Logos!)
                var enclosure = item.Elements().FirstOrDefault(x => x.Name.LocalName == "enclosure");
                if (enclosure != null) imageUrl = enclosure.Attribute("url")?.Value ?? string.Empty;

                // Wenn kein offizielles Bild da ist, besuchen wir heimlich die Webseite!
                if (string.IsNullOrEmpty(imageUrl) || imageUrl.Contains("1x1") || imageUrl.Contains("logo"))
                {
                    imageUrl = await FetchOpenGraphImageAsync(link);
                }

                // Android HTTPS Fix
                if (imageUrl.StartsWith("http://")) imageUrl = imageUrl.Replace("http://", "https://");

                // Hochwertiger Fallback, falls die Webseite wirklich nichts hat
                if (string.IsNullOrEmpty(imageUrl))
                {
                    imageUrl = "https://images.unsplash.com/photo-1585829365295-ab7cd400c167?w=600&q=80";
                }

                // --- 2. ZEIT BERECHNEN ---
                var pubDateStr = item.Elements().FirstOrDefault(x => x.Name.LocalName.ToLower().Contains("pubdate") || x.Name.LocalName.ToLower().Contains("date"))?.Value;
                DateTime pubDate = DateTime.Now;
                if (!string.IsNullOrEmpty(pubDateStr))
                {
                    pubDateStr = pubDateStr.Replace("MEZ", "+0100").Replace("MESZ", "+0200").Replace("GMT", "+0000");
                    DateTime.TryParse(pubDateStr, out pubDate);
                }

                articles.Add(new Article
                {
                    Title = cleanTitle,
                    Summary = cleanDesc,
                    OriginalUrl = link,
                    PublishedDate = pubDate,
                    SourceId = source.Id,
                    SourceName = source.Name,
                    ImageUrl = imageUrl
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lade-Fehler: {ex.Message}");
        }

        return articles;
    }

    // Die Geheimwaffe: Besucht die echte Webseite und klaut das echte Titelbild
    private async Task<string> FetchOpenGraphImageAsync(string url)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // Max 3 Sekunden warten
            var html = await _httpClient.GetStringAsync(url, cts.Token);
            var imgMatch = Regex.Match(html, @"<meta\s+(?:property|name)=[""']og:image[""']\s+content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            return imgMatch.Success ? imgMatch.Groups[1].Value : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
