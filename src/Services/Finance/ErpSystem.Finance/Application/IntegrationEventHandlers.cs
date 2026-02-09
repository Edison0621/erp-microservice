using MediatR;
using ErpSystem.Finance.Domain;

namespace ErpSystem.Finance.Application;

public class SalesIntegrationEventHandler(IMediator mediator) : INotificationHandler<ErpSystem.Sales.Domain.SalesIntegrationEvents.ShipmentCreatedIntegrationEvent>
{
    public async Task Handle(ErpSystem.Sales.Domain.SalesIntegrationEvents.ShipmentCreatedIntegrationEvent n, CancellationToken ct)
    {
        // Auto-create invoice when shipment is created
        string invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{n.ShipmentId.ToString()[..4]}";
        List<InvoiceLine> lines = n.Items.Select((x, index) => new InvoiceLine(
            (index + 1).ToString(), // LineNumber
            x.MaterialId,
            x.MaterialName,
            x.Quantity,
            100, // Price would ideally come from SaleOrder, using 100 as placeholder
            0.13m // 13% TaxRate
        )).ToList();

        await mediator.Send(new CreateInvoiceCommand(
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
