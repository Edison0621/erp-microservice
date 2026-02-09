using MediatR;
using Microsoft.Extensions.Logging;

namespace ErpSystem.BuildingBlocks.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        logger.LogInformation("Processing Request: {Name} {@Request}", requestName, request);

        TResponse response = await next(cancellationToken);

        logger.LogInformation("Processed Request: {Name}", requestName);

        return response;
    }
}
