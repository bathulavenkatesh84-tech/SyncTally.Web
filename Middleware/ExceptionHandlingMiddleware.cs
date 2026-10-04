using System.Diagnostics;
using System.Text.Json;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Middleware;

/// <summary>
/// Single place where unhandled exceptions end up: logged through IErrorLogService, then
/// answered with JSON for AJAX/JSON callers or the friendly /Home/Error page for browsers.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IErrorLogService errorLog)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // client went away – nothing to log or render
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex, context.Request.Path + context.Request.QueryString, context.User.Identity?.Name);

            if (context.Response.HasStarted) throw;

            string requestId = Activity.Current?.Id ?? context.TraceIdentifier;
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            if (WantsJson(context.Request))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    ok = false,
                    message = "Something went wrong on our side. Please try again.",
                    requestId
                }));
                return;
            }

            context.SetEndpoint(null);
            context.Items["RequestId"] = requestId;
            context.Request.Path = "/Home/Error";
            context.Request.QueryString = QueryString.Empty;
            await _next(context);
        }
    }

    private static bool WantsJson(HttpRequest r) =>
        r.Headers.XRequestedWith == "XMLHttpRequest" ||
        (r.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false) ||
        (r.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase));
}
