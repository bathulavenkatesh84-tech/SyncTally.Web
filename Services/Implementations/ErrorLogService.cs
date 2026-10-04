using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Services.Implementations;

/// <summary>
/// Writes to the daily text file (Flexidb.WriteError) first – it still works when the database is the thing
/// that failed – and then tries the site_error_log table. Logging never throws.
/// </summary>
public class ErrorLogService : IErrorLogService
{
    private readonly IErrorLogRepository _repo;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(IErrorLogRepository repo, ILogger<ErrorLogService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public Task LogAsync(Exception ex, string? requestPath, string? userName) =>
        LogAsync("Error", ex.TargetSite?.DeclaringType?.FullName ?? ex.GetType().Name,
                 ex.Message, requestPath, userName, ex.ToString());

    public Task LogAsync(string level, string source, string message, string? requestPath = null, string? userName = null) =>
        LogAsync(level, source, message, requestPath, userName, null);

    private async Task LogAsync(string level, string source, string message, string? path, string? user, string? stack)
    {
        try { Flexidb.WriteError(source, message); } catch { /* never throw from the logger */ }
        try
        {
            await _repo.InsertAsync(level, source, message, stack, path, user);
        }
        catch (Exception dbEx)
        {
            _logger.LogWarning(dbEx, "Could not write to site_error_log");
        }
    }
}
