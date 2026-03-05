using System.Text;
using System.Text.Json;
using AuraNews.Models;

namespace AuraNews.Services;

public class AIService
{
    private readonly HttpClient _httpClient;

    // Dein API-Key (Universell einsetzbar für alle Modelle)
    private const string ApiKey = "AIzaSyAqT96MRcrrEAJ-vJlSWt2lyM7qllbro1c";

    // Der stabile Endpunkt für Gemini 2.5 Flash (Vermeidet den 404-Fehler von 2.0)
    private const string ApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={ApiKey}";

    public AIService()
    {
        _httpClient = new HttpClient();
        // Timeout auf 10 Sekunden erhöht für detailliertere Zusammenfassungen
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<string> SummarizeAndTranslateAsync(string rawText, string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

        // Der neue "Fakten-Prompt": Erschwingt Zahlen, Daten und Details
        var prompt = $@"Du bist ein professioneller Nachrichten-Redakteur. 
Analysiere den folgenden Artikel und erstelle eine strukturierte, informative und präzise Zusammenfassung auf Deutsch.

WICHTIGE REGELN:
1. Verwende 2 bis 3 Überschriften (z.B. 'Die Fakten:', 'Zahlen & Details:', 'Hintergrund:').
2. Nenne UNBEDINGT konkrete Zahlen, Daten, Prozentangaben, Opferzahlen, Geldsummen oder Schadensberichte, falls diese im Text stehen. Lass keine harten Fakten weg!
3. Schreibe unter jeder Überschrift 2 bis 4 detaillierte Stichpunkte (mit einem Spiegelstrich '•' am Anfang).
4. Schreibe verständlich und informativ. Die Stichpunkte dürfen ruhig Details enthalten.
5. VERWENDE KEINE Markdown-Formatierungen! Benutze keine Sternchen (**) für Text. Schreibe Überschriften einfach als Text mit Doppelpunkt am Ende.
6. Antworte direkt mit dem fertigen Text.

Artikel: {rawText}";

        return await CallGeminiAsync(prompt);
    }

    public async Task<string> DetectTopicAsync(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return "Allgemein";

        var prompt = $"Kategorisiere den Text in EIN EINZIGES Wort (Sport, Politik, Wirtschaft, Technologie, Unterhaltung oder Allgemein): {rawText}";
        var topic = await CallGeminiAsync(prompt);

        // Fallback bei Fehlern
        if (topic.StartsWith("API-Fehler") || topic.StartsWith("System-Fehler")) return "Allgemein";

        return topic.Trim('.', ' ', '\n', '\r');
    }

    private async Task<string> CallGeminiAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(ApiUrl, jsonContent);

            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseString);
                var aiText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return aiText ?? "Zusammenfassung fehlgeschlagen.";
            }

            return $"API-Fehler ({response.StatusCode}): {responseString}";
        }
        catch (TaskCanceledException)
        {
            return "System-Fehler: Die KI hat zu lange gebraucht (Timeout).";
        }
        catch (Exception ex)
        {
            return $"System-Fehler: {ex.Message}";
        }
    }
}