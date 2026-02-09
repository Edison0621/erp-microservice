using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ErpSystem.Procurement.Application;
using ErpSystem.Procurement.Domain;
using ErpSystem.Inventory.Application;
using MediatR;
using ErpSystem.Inventory.Infrastructure;

namespace ErpSystem.IntegrationTests;

public class ProcurementToInventoryTests : IntegrationTestBase
{
    [Fact]
    public async Task GoodsReceipt_ShouldUpdateInventory()
    {
        WebApplicationFactory<Inventory.Program>? inventoryApp = null;
        WebApplicationFactory<Procurement.Program>? procurementApp = null;

        try 
        {
            // 1. Setup Apps
            inventoryApp = this.CreateInventoryApp();
            HttpClient inventoryClient = inventoryApp.CreateClient();
            
            TestEventBus testEventBus = new(inventoryClient, "/api/v1/inventory/integration/goods-received");
            procurementApp = this.CreateProcurementApp(testEventBus);

            IMediator mediatorProcurement = procurementApp.Services.GetRequiredService<IMediator>();
            IMediator mediatorInventory = inventoryApp.Services.GetRequiredService<IMediator>();
            
            // 2. Setup Data
            string materialId = "MAT-101";
            string warehouseId = "WH-MAIN";
            decimal quantity = 50m;
            
            // Initial check: stock should be 0 or null
            InventoryItemReadModel? initialStock = await mediatorInventory.Send(new GetInventoryItemQuery(warehouseId, "DEFAULT_BIN", materialId));
            Assert.True(initialStock == null || initialStock.OnHandQuantity == 0);

            // 3. Create PO
            Guid poId = await mediatorProcurement.Send(new CreatePoCommand(
                "SUPP-001", "Supplier 1", DateTime.UtcNow, "USD",
                [new PurchaseOrderLine("1", materialId, "MAT101", "Material 101", quantity, 0, 100, warehouseId, DateTime.UtcNow.AddDays(7))]
            ));

            // Progress PO to SentToSupplier status so receipt is allowed
            await mediatorProcurement.Send(new SubmitPoCommand(poId));
            await mediatorProcurement.Send(new ApprovePoCommand(poId, "MANAGER", "Approved"));
            await mediatorProcurement.Send(new SendPoCommand(poId, "PURCHASER", "Email"));
            
            // 4. Execute Goods Receipt in Procurement
            await mediatorProcurement.Send(new RecordReceiptCommand(
                poId, DateTime.UtcNow, "TESTER",
                [new ReceiptLine("1", quantity, warehouseId, "LOC-1", "Pass")]
            ));

            // 5. Verify in Inventory
            // The TestEventBus is direct, but MediatR might have a tiny delay if async (though normally sync in this setup)
            await Task.Delay(500); 

            InventoryItemReadModel? stockInfo = await mediatorInventory.Send(new GetInventoryItemQuery(warehouseId, "DEFAULT_BIN", materialId));
            
            Assert.NotNull(stockInfo);
            Assert.Equal(quantity, stockInfo.OnHandQuantity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACTUAL_ERROR: {ex.Message}");
            Console.WriteLine($"INNER_ERROR: {ex.InnerException?.Message}");
            Console.WriteLine($"STACK_TRACE: {ex.StackTrace}");
            throw;
        }
        finally
        {
            inventoryApp?.Dispose();
            procurementApp?.Dispose();
        }
    }
}
