using Microsoft.Extensions.Logging;

namespace AnkiCleaner;

public class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;

    public LoggingHandler(ILogger<LoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        // Log the Request
        var requestBody =
            request.Content != null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : "[Empty]";
        _logger.LogTrace(
            "HTTP Outgoing Request: {Method} {Uri}\nBody: {Body}",
            request.Method,
            request.RequestUri,
            requestBody
        );

        var response = await base.SendAsync(request, cancellationToken);

        // Log the Response
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogTrace(
            "HTTP Incoming Response: {StatusCode}\nBody: {Body}",
            response.StatusCode,
            responseBody
        );

        return response;
    }
}
