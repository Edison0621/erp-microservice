using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ErpSystem.Sales.Application;
using ErpSystem.Sales.Domain;
using ErpSystem.Inventory.Application;
using ErpSystem.Inventory.Infrastructure;
using ErpSystem.BuildingBlocks.EventBus;
using MediatR;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.IntegrationTests;

public class SalesToInventoryTests : IntegrationTestBase
{
    [Fact]
    public async Task OrderConfirmation_ShouldReserveInventory()
    {
        WebApplicationFactory<ErpSystem.Inventory.Program>? inventoryApp = null;
        WebApplicationFactory<ErpSystem.Sales.Program>? salesApp = null;

        try 
        {
            // 1. Setup Apps
            inventoryApp = CreateInventoryApp();
            var inventoryClient = inventoryApp.CreateClient();
            
            // Route Sales events to Inventory
            var testEventBus = new TestEventBus(inventoryClient, "/api/v1/inventory/integration/order-confirmed");
            salesApp = CreateSalesApp(testEventBus);

            var mediatorSales = salesApp.Services.GetRequiredService<IMediator>();
            var mediatorInventory = inventoryApp.Services.GetRequiredService<IMediator>();
            
            var materialId = "MAT-SALES-101";
            var warehouseId = "WH-SALES";
            var stockQuantity = 100m;
            var orderQuantity = 20m;
            
            // 2. Initialize Stock in Inventory
            await mediatorInventory.Send(new ReceiveStockCommand(
                warehouseId, "DEFAULT_BIN", materialId, stockQuantity, 10m, "INITIAL", "INIT-001", "TESTER"
            ));
            
            // Verify items exists
            var stockInfo = await mediatorInventory.Send(new GetInventoryItemQuery(warehouseId, "DEFAULT_BIN", materialId));
            Assert.NotNull(stockInfo);
            Assert.Equal(stockQuantity, stockInfo.OnHandQuantity);

            // 3. Create Sales Order
            var soId = await mediatorSales.Send(new CreateSOCommand(
                "CUST-001", "Customer 1", DateTime.UtcNow, "USD",
                new List<SalesOrderLine> { 
                    new SalesOrderLine("1", materialId, "MAT001", "Material 001", orderQuantity, 0, "EA", 150m, 0)
                }
            ));

            // 4. Confirm Sales Order -> Should trigger reservation via TestEventBus
            await mediatorSales.Send(new ConfirmSOCommand(soId, warehouseId));

            // 5. Verify Reservation in Inventory
            await Task.Delay(500); 

            var updatedStock = await mediatorInventory.Send(new GetInventoryItemQuery(warehouseId, "DEFAULT_BIN", materialId));
            Assert.NotNull(updatedStock);
            Assert.Equal(orderQuantity, updatedStock.ReservedQuantity);
            Assert.Equal(stockQuantity - orderQuantity, updatedStock.AvailableQuantity);
            
            // Also check reservation record
            using var scope = inventoryApp.Services.CreateScope();
            var readDb = scope.ServiceProvider.GetRequiredService<InventoryReadDbContext>();
            var reservation = await readDb.StockReservations.FirstOrDefaultAsync(r => r.InventoryItemId == updatedStock.Id);
            Assert.NotNull(reservation);
            Assert.Equal(orderQuantity, reservation.Quantity);
            Assert.Equal("SALES_ORDER", reservation.SourceType);
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
            salesApp?.Dispose();
        }
    }
}
