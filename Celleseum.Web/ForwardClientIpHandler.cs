using Microsoft.AspNetCore.Http;

namespace Celleseum.Web;

public sealed class ForwardClientIpHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = accessor.HttpContext;
        string? ip = null;

        // If behind a reverse proxy, prefer incoming X-Forwarded-For
        var xffIncoming = ctx?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xffIncoming))
        {
            ip = xffIncoming.Split(',')[0].Trim();
        }

        ip ??= ctx?.Connection?.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(ip))
        {
            request.Headers.Remove("X-Forwarded-For");
            request.Headers.Add("X-Forwarded-For", ip);
        }

        return base.SendAsync(request, cancellationToken);
    }
}