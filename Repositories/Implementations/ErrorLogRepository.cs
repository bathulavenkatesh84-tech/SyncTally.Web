using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Repositories.Implementations;

public class ErrorLogRepository : IErrorLogRepository
{
    private readonly IFlexidb _db;
    public ErrorLogRepository(IFlexidb db) => _db = db;

    public async Task InsertAsync(string level, string? source, string message, string? stackTrace, string? requestPath, string? userName)
    {
        await _db.ExecuteAsync(SqlHelper.Cmd(
            @"INSERT INTO cmr_admin.site_error_log (level, source, message, stack_trace, request_path, user_name)
              VALUES (@level, @source, @message, @stack, @path, @user)",
            ("level", level), ("source", Trunc(source, 200)), ("message", message), ("stack", stackTrace),
            ("path", Trunc(requestPath, 300)), ("user", Trunc(userName, 200))));
    }

    private static string? Trunc(string? s, int max) => s is null ? null : (s.Length <= max ? s : s[..max]);
}
