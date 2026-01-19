using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading_Assistant.Service.Configuration;
using Trading_Assistant.Service.Models;

namespace Trading_Assistant.Service.Services;

public class MarketDataService : IMarketDataService
{
    private readonly ILogger<MarketDataService> _logger;
    private readonly AlphaVantageConfig _config;
    private readonly HttpClient _httpClient;

    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60)
    };

    public MarketDataService(ILogger<MarketDataService> logger, AlphaVantageConfig config)
    {
        _logger = logger;
        _config = config;
        _httpClient = new HttpClient();
    }

    public async Task<MarketData> GetMarketDataAsync(Asset asset)
    {
        try
        {
            _logger.LogInformation("Fetching market data for {Symbol} from Alpha Vantage", asset.Symbol);

            // Alpha Vantage TIME_SERIES_DAILY endpoint
            var url = $"{_config.BaseUrl}?function=TIME_SERIES_DAILY&symbol={asset.Symbol}&apikey={_config.ApiKey}&outputsize=compact";

            _logger.LogInformation("Requesting URL: {Url}", url.Replace(_config.ApiKey, "***"));

            var content = await FetchWithRetryAsync(url, asset.Symbol);

            // Log first 500 chars for debugging
            _logger.LogInformation("Response preview: {Preview}", content.Length > 500 ? content.Substring(0, 500) : content);

            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Check for error message
            if (root.TryGetProperty("Error Message", out var errorMsg))
            {
                var error = errorMsg.GetString();
                _logger.LogError("Alpha Vantage error message: {Error}", error);
                throw new Exception($"Alpha Vantage error: {error}");
            }

            // Check for rate limit note
            if (root.TryGetProperty("Note", out var note))
            {
                var noteText = note.GetString();
                _logger.LogWarning("Alpha Vantage rate limit: {Note}", noteText);
                throw new Exception($"Alpha Vantage rate limit: {noteText}");
            }

            // Check for Information field (sometimes present in errors)
            if (root.TryGetProperty("Information", out var info))
            {
                var infoText = info.GetString();
                _logger.LogWarning("Alpha Vantage information: {Info}", infoText);
                // This might be a rate limit message
                if (infoText?.Contains("API call frequency") == true || infoText?.Contains("premium") == true)
                {
                    throw new Exception($"Alpha Vantage rate limit: {infoText}");
                }
            }

            // Get the time series data
            if (!root.TryGetProperty("Time Series (Daily)", out var timeSeries))
            {
                _logger.LogError("Missing 'Time Series (Daily)' property. Available properties: {Props}",
                    string.Join(", ", root.EnumerateObject().Select(p => p.Name)));
                throw new Exception($"No time series data available for {asset.Symbol}");
            }

            var history = new List<HistoricalPrice>();

            foreach (var item in timeSeries.EnumerateObject())
            {
                try
                {
                    var date = DateTime.Parse(item.Name);
                    var data = item.Value;

                    history.Add(new HistoricalPrice
                    {
                        Date = date,
                        Open = decimal.Parse(data.GetProperty("1. open").GetString() ?? "0"),
                        High = decimal.Parse(data.GetProperty("2. high").GetString() ?? "0"),
                        Low = decimal.Parse(data.GetProperty("3. low").GetString() ?? "0"),
                        Close = decimal.Parse(data.GetProperty("4. close").GetString() ?? "0"),
                        Volume = long.Parse(data.GetProperty("5. volume").GetString() ?? "0")
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing date {Date} for {Symbol}", item.Name, asset.Symbol);
                }
            }

            // Sort by date (oldest first)
            history = history.OrderBy(h => h.Date).ToList();

            // Take last 60 days
            history = history.TakeLast(60).ToList();

            if (!history.Any())
            {
                throw new Exception($"No valid historical data for {asset.Symbol}");
            }

            _logger.LogInformation("Parsed {Count} days of history for {Symbol}", history.Count, asset.Symbol);

            var latest = history.Last();
            var currentPrice = latest.Close;
            var latestVolume = latest.Volume;

            // Calculate variations
            var previousDay = history.Count > 1 ? history[^2] : latest;
            var previousWeek = history.Count > 5 ? history[^6] : history.First();
            var previousMonth = history.Count > 20 ? history[^21] : history.First();

            var marketData = new MarketData
            {
                Asset = asset,
                CurrentPrice = currentPrice,
                Volume = latestVolume,
                LastUpdate = latest.Date,
                High52Week = history.Max(x => x.High),
                Low52Week = history.Min(x => x.Low),
                History = history
            };

            marketData.DayChange = currentPrice - previousDay.Close;
            marketData.DayChangePercent = previousDay.Close > 0 ? (marketData.DayChange / previousDay.Close) * 100 : 0;

            marketData.WeekChange = currentPrice - previousWeek.Close;
            marketData.WeekChangePercent = previousWeek.Close > 0 ? (marketData.WeekChange / previousWeek.Close) * 100 : 0;

            marketData.MonthChange = currentPrice - previousMonth.Close;
            marketData.MonthChangePercent = previousMonth.Close > 0 ? (marketData.MonthChange / previousMonth.Close) * 100 : 0;

            _logger.LogInformation("Successfully fetched market data for {Symbol}: ${Price} ({Change:F2}%)",
                asset.Symbol, marketData.CurrentPrice, marketData.DayChangePercent);

            return marketData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching market data for {Symbol}", asset.Symbol);
            throw;
        }
    }

    public async Task<List<MarketData>> GetMarketDataForAssetsAsync(List<Asset> assets)
    {
        var marketDataList = new List<MarketData>();

        foreach (var asset in assets)
        {
            try
            {
                var marketData = await GetMarketDataAsync(asset);
                marketDataList.Add(marketData);

                // Alpha Vantage free tier: 5 requests per minute, 500 per day
                // Wait 12 seconds between requests to stay under 5/min
                if (asset != assets.Last())
                {
                    _logger.LogInformation("Waiting 12 seconds before next request...");
                    await Task.Delay(12000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping {Symbol} due to error", asset.Symbol);
            }
        }

        return marketDataList;
    }

    private async Task<string> FetchWithRetryAsync(string url, string symbol)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Alpha Vantage returned {StatusCode}: {Error}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Alpha Vantage API error {response.StatusCode} for {symbol}");
                }

                var content = await response.Content.ReadAsStringAsync();

                // Check for rate limit in response body
                if (content.Contains("API call frequency") || content.Contains("rate limit"))
                {
                    throw new RateLimitException($"Rate limit hit for {symbol}");
                }

                return content;
            }
            catch (Exception ex) when (ex is HttpRequestException or RateLimitException or TaskCanceledException)
            {
                lastException = ex;

                if (attempt < MaxRetries)
                {
                    var delay = RetryDelays[attempt];
                    _logger.LogWarning(
                        "Request failed for {Symbol} (attempt {Attempt}/{MaxRetries}). Retrying in {Delay} seconds. Error: {Error}",
                        symbol, attempt + 1, MaxRetries + 1, delay.TotalSeconds, ex.Message);

                    await Task.Delay(delay);
                }
                else
                {
                    _logger.LogError(
                        "Request failed for {Symbol} after {MaxRetries} retries. Error: {Error}",
                        symbol, MaxRetries + 1, ex.Message);
                }
            }
        }

        throw lastException ?? new Exception($"Failed to fetch data for {symbol} after {MaxRetries + 1} attempts");
    }

    private class RateLimitException : Exception
    {
        public RateLimitException(string message) : base(message) { }
    }
}
