using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using MediatR;
using ErpSystem.Finance.Application;
using ErpSystem.Finance.Domain;
using ErpSystem.Finance.Infrastructure;

namespace ErpSystem.IntegrationTests.Finance;

public class InvoiceLifecycleTests : IntegrationTestBase
{
    [Fact]
    public async Task CompleteInvoiceLifecycle_ShouldUpdateStatusAndStats()
    {
        WebApplicationFactory<ErpSystem.Finance.Program>? financeApp = null;

        try
        {
            // 1. Setup
            financeApp = this.CreateFinanceApp(new TestEventBus(null!, ""));
            IMediator mediator = financeApp.Services.GetRequiredService<IMediator>();

            // 2. Create Draft Invoice
            var createCmd = new CreateInvoiceCommand(
                "INV-TEST-001",
                InvoiceType.AccountsReceivable,
                "CUS-001",
                "Test Customer",
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                "CNY",
                new List<InvoiceLine>
                {
                    new InvoiceLine("1", "MAT-01", "Test Product", 10, 100, 0.13m)
                }
            );

            Guid invoiceId = await mediator.Send(createCmd);

            // Verify Draft
            InvoiceReadModel? invoice = await mediator.Send(new GetInvoiceQuery(invoiceId));
            invoice.Should().NotBeNull();
            invoice!.Status.Should().Be((int)InvoiceStatus.Draft);
            invoice.OutstandingAmount.Should().Be(1130m); // 1000 + 130 tax

            // 3. Issue Invoice
            await mediator.Send(new IssueInvoiceCommand(invoiceId));

            // Verify Issued
            // Wait for projection (in-memory is fast but async)
            await Task.Delay(100);
            invoice = await mediator.Send(new GetInvoiceQuery(invoiceId));
            invoice!.Status.Should().Be((int)InvoiceStatus.Issued);

            // 4. Record Partial Payment
            await mediator.Send(new RecordPaymentCommand(invoiceId, 500m, DateTime.UtcNow, PaymentMethod.BankTransfer, "REF-001"));

            // Verify Partial
            await Task.Delay(100);
            invoice = await mediator.Send(new GetInvoiceQuery(invoiceId));
            invoice!.Status.Should().Be((int)InvoiceStatus.PartiallyPaid);
            invoice.PaidAmount.Should().Be(500m);
            invoice.OutstandingAmount.Should().Be(630m);

            // 5. Record Remaining Payment
            await mediator.Send(new RecordPaymentCommand(invoiceId, 630m, DateTime.UtcNow, PaymentMethod.BankTransfer, "REF-002"));

            // Verify Fully Paid
            await Task.Delay(100);
            invoice = await mediator.Send(new GetInvoiceQuery(invoiceId));
            invoice!.Status.Should().Be((int)InvoiceStatus.FullyPaid);
            invoice.OutstandingAmount.Should().Be(0m);

            // 6. Check Dashboard Stats
            FinancialDashboardStats stats = await mediator.Send(new GetFinancialDashboardStatsQuery());
            stats.Should().NotBeNull();
            stats.ReconciledCount.Should().BeGreaterThanOrEqualTo(1);
            stats.OrderCount.Should().BeGreaterThanOrEqualTo(1);
            stats.TotalReceivable.Should().BeGreaterThanOrEqualTo(0); // Should be 0 if only this invoice exists and is paid, or >0 if others exist
        }
        finally
        {
            financeApp?.Dispose();
        }
    }
}
