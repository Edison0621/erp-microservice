using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ErpSystem.Finance.Application;
using ErpSystem.Finance.Domain;
using MediatR;

namespace ErpSystem.IntegrationTests;

public class GlTests : IntegrationTestBase
{
    [Fact]
    public async Task JournalEntry_ShouldUpdateTrialBalance()
    {
        WebApplicationFactory<ErpSystem.Finance.Program>? financeApp = null;

        try 
        {
            // 1. Setup App
            financeApp = this.CreateFinanceApp(new TestEventBus(null, ""));
            IMediator mediator = financeApp.Services.GetRequiredService<IMediator>();

            // 2. Define Accounts
            Guid cashAccountId = await mediator.Send(new DefineAccountCommand(
                "1001", "Cash", AccountType.Asset, AccountClass.Current, null, BalanceType.Debit, "USD"));
            
            Guid equityAccountId = await mediator.Send(new DefineAccountCommand(
                "3001", "Owner Equity", AccountType.Equity, AccountClass.NonCurrent, null, BalanceType.Credit, "USD"));

            // 3. Define & Open Period
            Guid periodId = await mediator.Send(new DefineFinancialPeriodCommand(2026, 1, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)));
            // Period is open by default upon definition in our simplified logic? 
            // Checking Domain logic: "Define" sets IsClosed=false. So yes, open.

            // 4. Create Journal Entry (Draft)
            Guid jeId = await mediator.Send(new CreateJournalEntryCommand(
                "JE-001", 
                DateTime.UtcNow, 
                DateTime.UtcNow, 
                "Initial Investment", 
                JournalEntrySource.Manual, 
                null,
                [
                    new JournalEntryLineDto(cashAccountId, "Invest Cash", 1000m, 0),
                    new JournalEntryLineDto(equityAccountId, "Owner Capital", 0, 1000m)
                ]
            ));

            // Verify Draft Status
            JournalEntryDetailDto? jeDetail = await mediator.Send(new GetJournalEntryQuery(jeId));
            Assert.NotNull(jeDetail);
            Assert.Equal((int)JournalEntryStatus.Draft, jeDetail.Header.Status);

            // 5. Post Journal Entry
            await mediator.Send(new PostJournalEntryCommand(jeId));

            // Verify Posted Status
            // Wait for projection
            await Task.Delay(200);
            jeDetail = await mediator.Send(new GetJournalEntryQuery(jeId));
            Assert.Equal((int)JournalEntryStatus.Posted, jeDetail.Header.Status);

            // 6. Check Trial Balance
            List<TrialBalanceLineDto> tb = await mediator.Send(new GetTrialBalanceQuery(DateTime.UtcNow));
            
            Assert.Collection(tb.OrderBy(x => x.AccountCode), 
                c => {
                    Assert.Equal("1001", c.AccountCode);
                    Assert.Equal(1000m, c.Debit);
                    Assert.Equal(0m, c.Credit);
                },
                e => {
                    Assert.Equal("3001", e.AccountCode);
                    Assert.Equal(0m, e.Debit);
                    Assert.Equal(1000m, e.Credit);
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex}");
            throw;
        }
        finally
        {
            financeApp?.Dispose();
        }
    }
}
