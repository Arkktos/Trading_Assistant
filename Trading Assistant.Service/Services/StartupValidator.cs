using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Trading_Assistant.Service.Configuration;

namespace Trading_Assistant.Service.Services;

public class StartupValidator
{
    private readonly ILogger<StartupValidator> _logger;
    private readonly AppConfig _config;

    public StartupValidator(ILogger<StartupValidator> logger, AppConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<StartupValidationResult> ValidateAsync()
    {
        var result = new StartupValidationResult();

        _logger.LogInformation("Starting configuration validation...");

        // Validate Claude CLI
        var claudeResult = await ValidateClaudeCliAsync();
        result.ClaudeCliAvailable = claudeResult.Success;
        if (!claudeResult.Success)
        {
            result.Errors.Add($"Claude CLI: {claudeResult.Message}");
        }
        else
        {
            _logger.LogInformation("Claude CLI: {Message}", claudeResult.Message);
        }

        // Validate Alpha Vantage configuration
        var alphaResult = ValidateAlphaVantageConfig();
        result.AlphaVantageConfigValid = alphaResult.Success;
        if (!alphaResult.Success)
        {
            result.Errors.Add($"Alpha Vantage: {alphaResult.Message}");
        }
        else
        {
            _logger.LogInformation("Alpha Vantage config: Valid");
        }

        // Validate watched assets
        if (_config.Trading.WatchedAssets.Count == 0)
        {
            result.Warnings.Add("No watched assets configured");
        }
        else
        {
            _logger.LogInformation("Watched assets: {Count} configured", _config.Trading.WatchedAssets.Count);
        }

        // Log summary
        if (result.Errors.Any())
        {
            _logger.LogError("Validation completed with {ErrorCount} error(s)", result.Errors.Count);
            foreach (var error in result.Errors)
            {
                _logger.LogError("  - {Error}", error);
            }
        }

        if (result.Warnings.Any())
        {
            _logger.LogWarning("Validation completed with {WarningCount} warning(s)", result.Warnings.Count);
            foreach (var warning in result.Warnings)
            {
                _logger.LogWarning("  - {Warning}", warning);
            }
        }

        if (!result.Errors.Any() && !result.Warnings.Any())
        {
            _logger.LogInformation("All validations passed successfully");
        }

        return result;
    }

    private async Task<(bool Success, string Message)> ValidateClaudeCliAsync()
    {
        try
        {
            var claudePath = ClaudeCliResolver.Resolve();
            _logger.LogInformation("Resolved Claude CLI path: {Path}", claudePath);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = claudePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);

            if (process.ExitCode == 0)
            {
                var version = output.Trim().Split('\n').FirstOrDefault() ?? "unknown version";
                return (true, $"Available ({version})");
            }

            return (false, "CLI returned non-zero exit code");
        }
        catch (OperationCanceledException)
        {
            return (false, "CLI check timed out after 10 seconds");
        }
        catch (Exception ex)
        {
            return (false, $"CLI not found or not accessible: {ex.Message}");
        }
    }

    private (bool Success, string Message) ValidateAlphaVantageConfig()
    {
        var alpha = _config.AlphaVantage;

        if (string.IsNullOrWhiteSpace(alpha.ApiKey))
        {
            return (false, "API key not configured");
        }

        if (alpha.ApiKey == "YOUR_KEY" || alpha.ApiKey == "demo")
        {
            return (false, "API key appears to be a placeholder");
        }

        return (true, "API key configured");
    }
}

public class StartupValidationResult
{
    public bool ClaudeCliAvailable { get; set; }
    public bool AlphaVantageConfigValid { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public bool HasCriticalErrors => !ClaudeCliAvailable || !AlphaVantageConfigValid;
    public bool IsFullyValid => !Errors.Any() && !Warnings.Any();
}
