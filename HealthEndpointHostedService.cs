using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace NHSUKMCP;

/// <summary>
/// Extremely lightweight health endpoint using HttpListener (no ASP.NET dependency) on port 8080.
/// Provides /healthz and /ready returning small JSON payloads.
/// </summary>
public class HealthEndpointHostedService : IHostedService
{
    private readonly ILogger<HealthEndpointHostedService> _logger;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public HealthEndpointHostedService(ILogger<HealthEndpointHostedService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _listener = new HttpListener();
            // Listen on all interfaces port 8080
            _listener.Prefixes.Add("http://*:8080/");
            _listener.Start();
            _logger.LogInformation("Health listener started on :8080");
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loopTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start health listener (continuing without health endpoints)");
        }
        return Task.CompletedTask;
    }

    private async Task ListenLoopAsync(CancellationToken token)
    {
        if (_listener == null) return;
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext? ctx = null;
            try
            {
                ctx = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested) break;
                _logger.LogDebug(ex, "Health listener accept loop error");
                continue;
            }
            _ = Task.Run(() => HandleRequestAsync(ctx), token);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath ?? string.Empty;
            object payload = path switch
            {
                "/ready" => new { status = "ready" },
                "/healthz" => new { status = "ok" },
                _ => new { status = "unknown" }
            };
            var json = JsonSerializer.Serialize(payload);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;
            using var writer = new StreamWriter(context.Response.OutputStream);
            await writer.WriteAsync(json);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error handling health request");
        }
        finally
        {
            try { context.Response.OutputStream.Close(); } catch { }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            _logger.LogInformation("Health listener stopped");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error stopping health listener");
        }
        return Task.CompletedTask;
    }
}
