using System.Text.RegularExpressions;
using Serilog.Context;

namespace NexusPay.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private static readonly Regex ValidId = new("^[A-Za-z0-9_-]{1,128}$", RegexOptions.CultureInvariant);
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemName = "NexusPay.CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = supplied is not null && ValidId.IsMatch(supplied)
            ? supplied : Guid.NewGuid().ToString("N");
        context.Items[ItemName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        using (LogContext.PushProperty("CorrelationId", correlationId))
            await next(context);
    }
}
