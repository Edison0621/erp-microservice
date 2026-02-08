using MediatR;
using ErpSystem.Sales.Domain;
using ErpSystem.Finance.Domain;

namespace ErpSystem.Finance.Application;

public class SalesIntegrationEventHandler : INotificationHandler<ErpSystem.Sales.Domain.SalesIntegrationEvents.ShipmentCreatedIntegrationEvent>
{
    private readonly IMediator _mediator;

    public SalesIntegrationEventHandler(IMediator mediator) => _mediator = mediator;

    public async Task Handle(ErpSystem.Sales.Domain.SalesIntegrationEvents.ShipmentCreatedIntegrationEvent n, CancellationToken ct)
    {
        // Auto-create invoice when shipment is created
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{n.ShipmentId.ToString()[..4]}";
        var lines = n.Items.Select((x, index) => new InvoiceLine(
            (index + 1).ToString(), // LineNumber
            x.MaterialId,
            x.MaterialName,
            x.Quantity,
            100, // Price would ideally come from SaleOrder, using 100 as placeholder
            0.13m // 13% TaxRate
        )).ToList();

        await _mediator.Send(new CreateInvoiceCommand(
            invoiceNumber,
            InvoiceType.AccountsReceivable,
            "CUSTOMER-ID", // Ideally lookup from SalesOrder
            "CUSTOMER-NAME",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            "USD",
            lines
        ), ct);
    }
}
