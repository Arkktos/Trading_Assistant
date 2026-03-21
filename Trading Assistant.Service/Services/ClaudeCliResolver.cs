namespace Trading_Assistant.Service.Services;

public static class ClaudeCliResolver
{
    private static string? _cachedPath;

    /// <summary>
    /// Resolves the full path to the Claude CLI executable.
    /// When running as a Windows Service under SYSTEM, the user PATH is not available,
    /// so we check common installation locations.
    /// </summary>
    public static string Resolve()
    {
        if (_cachedPath != null)
            return _cachedPath;

        // 1. Check if "claude" is directly available on PATH
        var pathResult = FindOnPath("claude");
        if (pathResult != null)
        {
            _cachedPath = pathResult;
            return _cachedPath;
        }

        // 2. Check common installation locations for all user profiles
        var usersDir = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
                       ?? @"C:\Users";

        foreach (var userDir in Directory.GetDirectories(usersDir))
        {
            var candidates = new[]
            {
                Path.Combine(userDir, ".local", "bin", "claude.exe"),
                Path.Combine(userDir, ".local", "bin", "claude"),
                Path.Combine(userDir, "AppData", "Local", "Programs", "claude", "claude.exe"),
                Path.Combine(userDir, "AppData", "Roaming", "npm", "claude.cmd"),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    _cachedPath = candidate;
                    return _cachedPath;
                }
            }
        }

        // 3. Check global locations
        var globalCandidates = new[]
        {
            @"C:\Program Files\Claude\claude.exe",
            @"C:\Program Files (x86)\Claude\claude.exe",
        };

        foreach (var candidate in globalCandidates)
        {
            if (File.Exists(candidate))
            {
                _cachedPath = candidate;
                return _cachedPath;
            }
        }

        // Fallback to just "claude" and let it fail with a clear error
        _cachedPath = "claude";
        return _cachedPath;
    }

    private static string? FindOnPath(string executable)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var extensions = new[] { "", ".exe", ".cmd", ".bat" };

        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            foreach (var ext in extensions)
            {
                var fullPath = Path.Combine(dir, executable + ext);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        return null;
    }
}
