using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ErpSystem.Sales.Application;
using ErpSystem.Sales.Domain;
using ErpSystem.Inventory.Application;
using ErpSystem.Inventory.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.IntegrationTests;

public class SalesToInventoryTests : IntegrationTestBase
{
    [Fact]
    public async Task OrderConfirmation_ShouldReserveInventory()
    {
        WebApplicationFactory<Inventory.Program>? inventoryApp = null;
        WebApplicationFactory<Sales.Program>? salesApp = null;

        try 
        {
            // 1. Setup Apps
            inventoryApp = this.CreateInventoryApp();
            HttpClient inventoryClient = inventoryApp.CreateClient();
            
            // Route Sales events to Inventory
            TestEventBus testEventBus = new TestEventBus(inventoryClient, "/api/v1/inventory/integration/order-confirmed");
            salesApp = this.CreateSalesApp(testEventBus);

            IMediator mediatorSales = salesApp.Services.GetRequiredService<IMediator>();
            IMediator mediatorInventory = inventoryApp.Services.GetRequiredService<IMediator>();
            
            string materialId = "MAT-SALES-101";
            string warehouseId = "WH-SALES";
            decimal stockQuantity = 100m;
            decimal orderQuantity = 20m;
            
            // 2. Initialize Stock in Inventory
            await mediatorInventory.Send(new ReceiveStockCommand(
                warehouseId, "DEFAULT_BIN", materialId, stockQuantity, 10m, "INITIAL", "INIT-001", "TESTER"
            ));
            
            // Verify items exists
            InventoryItemReadModel? stockInfo = await mediatorInventory.Send(new GetInventoryItemQuery(warehouseId, "DEFAULT_BIN", materialId));
            Assert.NotNull(stockInfo);
            Assert.Equal(stockQuantity, stockInfo.OnHandQuantity);

            // 3. Create Sales Order
            Guid soId = await mediatorSales.Send(new CreateSoCommand(
                "CUST-001", "Customer 1", DateTime.UtcNow, "USD",
                [new SalesOrderLine("1", materialId, "MAT001", "Material 001", orderQuantity, 0, "EA", 150m, 0)]
            ));

            // 4. Confirm Sales Order -> Should trigger reservation via TestEventBus
            await mediatorSales.Send(new ConfirmSoCommand(soId, warehouseId));

            // 5. Verify Reservation in Inventory
            await Task.Delay(500); 

            InventoryItemReadModel? updatedStock = await mediatorInventory.Send(new GetInventoryItemQuery(warehouseId, "DEFAULT_BIN", materialId));
            Assert.NotNull(updatedStock);
            Assert.Equal(orderQuantity, updatedStock.ReservedQuantity);
            Assert.Equal(stockQuantity - orderQuantity, updatedStock.AvailableQuantity);
            
            // Also check reservation record
            using IServiceScope scope = inventoryApp.Services.CreateScope();
            InventoryReadDbContext readDb = scope.ServiceProvider.GetRequiredService<InventoryReadDbContext>();
            StockReservationReadModel? reservation = await readDb.StockReservations.FirstOrDefaultAsync(r => r.InventoryItemId == updatedStock.Id);
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
