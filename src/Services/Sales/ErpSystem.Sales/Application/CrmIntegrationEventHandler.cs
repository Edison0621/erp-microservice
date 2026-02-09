using MediatR;
using ErpSystem.CRM.Domain;
using ErpSystem.Sales.Domain;

namespace ErpSystem.Sales.Application;

public class CrmIntegrationEventHandler(IMediator mediator, ILogger<CrmIntegrationEventHandler> logger) :
    INotificationHandler<CrmIntegrationEvents.OpportunityWonIntegrationEvent>
{
    public async Task Handle(CrmIntegrationEvents.OpportunityWonIntegrationEvent @event, CancellationToken ct)
    {
        logger.LogInformation("Creating Sales Order from Won Opportunity: {OpportunityNumber}", @event.OpportunityNumber);

        List<SalesOrderLine> lines =
        [
            new(
                "1",
                "OFF-SERVICE",
                "SVC-001",
                $"Won Opportunity: {@event.OpportunityName}",
                1,
                0,
                "Unit",
                @event.FinalValue,
                0
            )
        ];

        CreateSoCommand command = new(
            @event.CustomerId ?? "CUST-UNKNOWN",
            @event.CustomerName ?? "Unknown Customer",
            DateTime.UtcNow,
            @event.Currency,
            lines
        );

        await mediator.Send(command, ct);

        logger.LogInformation("Sales Order triggered for Opportunity {OpportunityNumber}", @event.OpportunityNumber);
    }
}
