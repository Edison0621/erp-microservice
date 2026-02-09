using MediatR;

namespace ErpSystem.BuildingBlocks.Contracts;

public class CrmIntegrationEvents
{
    public record OpportunityWonIntegrationEvent(
        Guid OpportunityId,
        string OpportunityNumber,
        string OpportunityName,
        string? CustomerId,
        string? CustomerName,
        decimal FinalValue,
        string Currency,
        string? AssignedToUserId,
        DateTime WonDate
    ) : INotification;
}
