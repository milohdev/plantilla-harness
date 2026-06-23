using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Logistics.Application.Common.Behaviors;

/// <summary>
/// Registra inicio, fin y duración de cada request de MediatR. El correlation
/// id viaja en el scope de logging (lo abre el CorrelationIdMiddleware).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds} ms",
                requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Request {RequestName} failed after {ElapsedMilliseconds} ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
