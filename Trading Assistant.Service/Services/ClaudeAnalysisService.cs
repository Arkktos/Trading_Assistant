using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading_Assistant.Service.Configuration;
using Trading_Assistant.Service.Models;

namespace Trading_Assistant.Service.Services;

public class ClaudeAnalysisService : IClaudeAnalysisService
{
    private readonly ILogger<ClaudeAnalysisService> _logger;
    private readonly ClaudeConfig _config;

    public ClaudeAnalysisService(ILogger<ClaudeAnalysisService> logger, ClaudeConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<AnalysisResult> AnalyzeMarketDataAsync(List<MarketData> marketDataList, decimal availableCapital, string riskProfile)
    {
        try
        {
            _logger.LogInformation("Starting Claude Code analysis for {Count} assets", marketDataList.Count);

            // Create a temporary file with market data
            var marketDataFile = Path.Combine(Path.GetTempPath(), $"market-data-{DateTime.Now:yyyyMMdd-HHmmss}.md");
            var marketDataContent = BuildMarketDataMarkdown(marketDataList, availableCapital, riskProfile);
            await File.WriteAllTextAsync(marketDataFile, marketDataContent);

            _logger.LogInformation("Market data written to {File}", marketDataFile);

            // Build the prompt for Claude
            var prompt = BuildAnalysisPrompt();

            // Call Claude Code CLI
            var analysisText = await CallClaudeCodeAsync(prompt, marketDataFile);

            // Parse the response
            var analysisResult = ParseAnalysisResponse(analysisText, marketDataList);

            // Cleanup temp file
            try
            {
                File.Delete(marketDataFile);
            }
            catch
            {
                // Ignore cleanup errors
            }

            return analysisResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Claude Code analysis");
            throw;
        }
    }

    private async Task<string> CallClaudeCodeAsync(string prompt, string marketDataFile)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = ClaudeCliResolver.Resolve(),
            ArgumentList = { prompt },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(marketDataFile)
        };

        _logger.LogInformation("Calling Claude Code CLI...");

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogError("Claude Code exited with code {Code}. Error: {Error}", process.ExitCode, error);
            throw new Exception($"Claude Code failed with exit code {process.ExitCode}: {error}");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new Exception("Claude Code returned empty output");
        }

        _logger.LogInformation("Received analysis from Claude Code ({Length} characters)", output.Length);

        return output;
    }

    private string BuildMarketDataMarkdown(List<MarketData> marketDataList, decimal availableCapital, string riskProfile)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Données de Marché - Analyse Trading");
        sb.AppendLine();
        sb.AppendLine($"**Date:** {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"**Capital disponible:** ${availableCapital:N2}");
        sb.AppendLine($"**Profil de risque:** {riskProfile}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var data in marketDataList)
        {
            sb.AppendLine($"## {data.Asset.Name} ({data.Asset.Symbol})");
            sb.AppendLine();
            sb.AppendLine("### Prix et Volume");
            sb.AppendLine($"- **Prix actuel:** ${data.CurrentPrice:F2}");
            sb.AppendLine($"- **Volume:** {data.Volume:N0}");
            sb.AppendLine();
            sb.AppendLine("### Variations");
            sb.AppendLine($"- **Jour:** ${data.DayChange:F2} ({data.DayChangePercent:F2}%)");
            sb.AppendLine($"- **Semaine:** ${data.WeekChange:F2} ({data.WeekChangePercent:F2}%)");
            sb.AppendLine($"- **Mois:** ${data.MonthChange:F2} ({data.MonthChangePercent:F2}%)");
            sb.AppendLine();
            sb.AppendLine("### Range 52 semaines");
            sb.AppendLine($"- **Plus haut:** ${data.High52Week:F2}");
            sb.AppendLine($"- **Plus bas:** ${data.Low52Week:F2}");
            sb.AppendLine();

            if (data.History.Any())
            {
                sb.AppendLine("### Historique récent (derniers 5 jours)");
                foreach (var h in data.History.TakeLast(5))
                {
                    sb.AppendLine($"- {h.Date:dd/MM}: Open ${h.Open:F2}, Close ${h.Close:F2}, High ${h.High:F2}, Low ${h.Low:F2}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string BuildAnalysisPrompt()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Analyse les données de marché du fichier market-data-*.md dans ce répertoire.");
        sb.AppendLine();
        sb.AppendLine("Tu es un assistant d'analyse de marchés financiers. Fournis une analyse factuelle basée sur les données, pas des conseils personnalisés.");
        sb.AppendLine();
        sb.AppendLine("Fournis:");
        sb.AppendLine("1. Un résumé général du marché (2-3 phrases)");
        sb.AppendLine("2. Les opportunités potentielles avec explications factuelles (patterns techniques, contexte)");
        sb.AppendLine("3. Une évaluation des risques");
        sb.AppendLine("4. Le sentiment de marché général");
        sb.AppendLine("5. 3-5 observations clés");
        sb.AppendLine();
        sb.AppendLine("Pour chaque opportunité, indique:");
        sb.AppendLine("- Le symbole de l'actif");
        sb.AppendLine("- La direction (Buy/Sell/Hold)");
        sb.AppendLine("- Les raisons factuelles");
        sb.AppendLine("- Le niveau de confiance (Low/Medium/High)");
        sb.AppendLine("- L'horizon temporel (Short-term/Medium-term/Long-term)");
        sb.AppendLine("- Le prix cible (targetPrice) basé sur l'analyse technique");
        sb.AppendLine("- Le stop loss recommandé basé sur les supports/résistances");
        sb.AppendLine();
        sb.AppendLine("Réponds en format JSON avec cette structure:");
        sb.AppendLine(@"{
  ""summary"": ""résumé général"",
  ""opportunities"": [
    {
      ""symbol"": ""AAPL"",
      ""direction"": ""Buy"",
      ""reason"": ""raison détaillée"",
      ""confidence"": ""High"",
      ""timeframe"": ""Medium-term"",
      ""targetPrice"": 185.50,
      ""stopLoss"": 172.00
    }
  ],
  ""riskAssessment"": ""évaluation des risques"",
  ""marketSentiment"": ""sentiment général"",
  ""keyObservations"": [""observation 1"", ""observation 2""]
}");

        return sb.ToString();
    }

    private AnalysisResult ParseAnalysisResponse(string analysisText, List<MarketData> marketDataList)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = analysisText.IndexOf('{');
            var jsonEnd = analysisText.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = analysisText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var jsonDoc = JsonDocument.Parse(jsonText);
                var root = jsonDoc.RootElement;

                var result = new AnalysisResult
                {
                    Summary = root.TryGetProperty("summary", out var summaryProp) ? summaryProp.GetString() ?? string.Empty : string.Empty,
                    RiskAssessment = root.TryGetProperty("riskAssessment", out var riskProp) ? riskProp.GetString() ?? string.Empty : string.Empty,
                    MarketSentiment = root.TryGetProperty("marketSentiment", out var sentimentProp) ? sentimentProp.GetString() ?? string.Empty : string.Empty,
                    AnalysisDate = DateTime.Now,
                    Opportunities = new List<TradingOpportunity>(),
                    KeyObservations = new List<string>()
                };

                if (root.TryGetProperty("opportunities", out var oppsProp) && oppsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var oppElement in oppsProp.EnumerateArray())
                    {
                        var symbol = oppElement.TryGetProperty("symbol", out var symProp) ? symProp.GetString() ?? string.Empty : string.Empty;
                        var asset = marketDataList.FirstOrDefault(m => m.Asset.Symbol == symbol)?.Asset;

                        if (asset != null)
                        {
                            var opportunity = new TradingOpportunity
                            {
                                Asset = asset,
                                Direction = oppElement.TryGetProperty("direction", out var dirProp) ? dirProp.GetString() ?? string.Empty : string.Empty,
                                Reason = oppElement.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() ?? string.Empty : string.Empty,
                                ConfidenceLevel = oppElement.TryGetProperty("confidence", out var confProp) ? confProp.GetString() ?? string.Empty : string.Empty,
                                Timeframe = oppElement.TryGetProperty("timeframe", out var timeProp) ? timeProp.GetString() ?? string.Empty : string.Empty,
                                TargetPrice = ParseDecimalProperty(oppElement, "targetPrice"),
                                StopLoss = ParseDecimalProperty(oppElement, "stopLoss")
                            };

                            result.Opportunities.Add(opportunity);
                        }
                    }
                }

                if (root.TryGetProperty("keyObservations", out var obsProp) && obsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var obsElement in obsProp.EnumerateArray())
                    {
                        var observation = obsElement.GetString();
                        if (!string.IsNullOrEmpty(observation))
                        {
                            result.KeyObservations.Add(observation);
                        }
                    }
                }

                return result;
            }
            else
            {
                // No JSON found, return raw text as summary
                return new AnalysisResult
                {
                    Summary = analysisText,
                    AnalysisDate = DateTime.Now,
                    Opportunities = new List<TradingOpportunity>(),
                    KeyObservations = new List<string>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing JSON response, returning raw text");
            return new AnalysisResult
            {
                Summary = analysisText,
                AnalysisDate = DateTime.Now,
                Opportunities = new List<TradingOpportunity>(),
                KeyObservations = new List<string>()
            };
        }
    }

    private static decimal? ParseDecimalProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(prop.GetString(), out var parsed) => parsed,
            _ => null
        };
    }
}
