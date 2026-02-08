using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ErpSystem.Procurement.Application;
using ErpSystem.Procurement.Domain;
using ErpSystem.Inventory.Application;
using ErpSystem.BuildingBlocks.EventBus;
using MediatR;
using System.Net.Http.Json;

namespace ErpSystem.IntegrationTests;

public class ProcurementToInventoryTests : IntegrationTestBase
{
    [Fact]
    public async Task GoodsReceipt_ShouldUpdateInventory()
    {
        WebApplicationFactory<ErpSystem.Inventory.Program>? inventoryApp = null;
        WebApplicationFactory<ErpSystem.Procurement.Program>? procurementApp = null;

        try 
        {
            // 1. Setup Apps
            inventoryApp = CreateInventoryApp();
            var inventoryClient = inventoryApp.CreateClient();
            
            var testEventBus = new TestEventBus(inventoryClient, "/api/v1/inventory/integration/goods-received");
            procurementApp = CreateProcurementApp(testEventBus);

            var mediatorProcurement = procurementApp.Services.GetRequiredService<IMediator>();
            var mediatorInventory = inventoryApp.Services.GetRequiredService<IMediator>();
            
            // 2. Setup Data
            var materialId = "MAT-101";
            var warehouseId = "WH-MAIN";
            var quantity = 50m;
            
            // Initial check: stock should be 0 or null
            var initialStock = await mediatorInventory.Send(new GetInventoryItemQuery(warehouseId, "DEFAULT_BIN", materialId));
            Assert.True(initialStock == null || initialStock.OnHandQuantity == 0);

            // 3. Create PO
            var poId = await mediatorProcurement.Send(new CreatePOCommand(
                "SUPP-001", "Supplier 1", DateTime.UtcNow, "USD",
                new List<PurchaseOrderLine> { 
                    new PurchaseOrderLine("1", materialId, "MAT101", "Material 101", quantity, 0, 100, warehouseId, DateTime.UtcNow.AddDays(7))
                }
            ));

            // Progress PO to SentToSupplier status so receipt is allowed
            await mediatorProcurement.Send(new SubmitPOCommand(poId));
            await mediatorProcurement.Send(new ApprovePOCommand(poId, "MANAGER", "Approved"));
            await mediatorProcurement.Send(new SendPOCommand(poId, "PURCHASER", "Email"));
            
            // 4. Execute Goods Receipt in Procurement
            await mediatorProcurement.Send(new RecordReceiptCommand(
                poId, DateTime.UtcNow, "TESTER",
                new List<ErpSystem.Procurement.Domain.ReceiptLine> { 
                    new ErpSystem.Procurement.Domain.ReceiptLine("1", quantity, warehouseId, "LOC-1", "Pass") 
                }
            ));

            // 5. Verify in Inventory
            // The TestEventBus is direct, but MediatR might have a tiny delay if async (though normally sync in this setup)
            await Task.Delay(500); 

            var stockInfo = await mediatorInventory.Send(new GetInventoryItemQuery(warehouseId, "DEFAULT_BIN", materialId));
            
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
