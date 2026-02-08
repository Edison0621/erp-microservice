using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ErpSystem.Production.Application;
using ErpSystem.Production.Domain;
using ErpSystem.Inventory.Application;
using ErpSystem.Inventory.Infrastructure;
using ErpSystem.BuildingBlocks.EventBus;
using MediatR;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.IntegrationTests;

public class ProductionToInventoryTests : IntegrationTestBase
{
    [Fact]
    public async Task ProductionMaterialIssue_ShouldUpdateInventory()
    {
        WebApplicationFactory<ErpSystem.Inventory.Program>? inventoryApp = null;
        WebApplicationFactory<ErpSystem.Production.Program>? productionApp = null;

        try 
        {
            // 1. Setup Apps
            inventoryApp = CreateInventoryApp();
            var inventoryClient = inventoryApp.CreateClient();
            
            // Route Production Material Issued events to Inventory
            var testEventBus = new TestEventBus(inventoryClient, "/api/v1/inventory/integration/production-material-issued");
            productionApp = CreateProductionApp(testEventBus);

            var mediatorProduction = productionApp.Services.GetRequiredService<IMediator>();
            var mediatorInventory = inventoryApp.Services.GetRequiredService<IMediator>();
            
            var materialId = "RM-101";
            var warehouseId = "WH-PROD";
            var initialStock = 100m;
            var consumptionQuantity = 30m;
            
            // 2. Initialize Stock in Inventory
            await mediatorInventory.Send(new ReceiveStockCommand(
                warehouseId, "DEFAULT_BIN", materialId, initialStock, 10m, "INITIAL", "INIT-001", "TESTER"
            ));

            // 3. Create and Release Production Order
            var prdId = await mediatorProduction.Send(new CreateProductionOrderCommand(
                "FG-101", "FG101", "Finished Good 101", 50m
            ));
            await mediatorProduction.Send(new ReleaseProductionOrderCommand(prdId));

            // 4. Consume Material -> Should trigger stock issue via TestEventBus
            await mediatorProduction.Send(new ConsumeMaterialCommand(
                prdId, materialId, warehouseId, consumptionQuantity, "PRODUCTION_USER"
            ));

            // 5. Verify in Inventory
            await Task.Delay(500); 

            var stockInfo = await mediatorInventory.Send(new GetInventoryItemQuery(warehouseId, "DEFAULT_BIN", materialId));
            Assert.NotNull(stockInfo);
            Assert.Equal(initialStock - consumptionQuantity, stockInfo.OnHandQuantity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACTUAL_ERROR: {ex.Message}");
            Console.WriteLine($"STACK_TRACE: {ex.StackTrace}");
            throw;
        }
        finally
        {
            inventoryApp?.Dispose();
            productionApp?.Dispose();
        }
    }

    [Fact]
    public async Task ProductionCompletion_ShouldUpdateInventory()
    {
        WebApplicationFactory<ErpSystem.Inventory.Program>? inventoryApp = null;
        WebApplicationFactory<ErpSystem.Production.Program>? productionApp = null;

        try 
        {
            // 1. Setup Apps
            inventoryApp = CreateInventoryApp();
            var inventoryClient = inventoryApp.CreateClient();
            
            // Route Production Completed events to Inventory
            var testEventBus = new TestEventBus(inventoryClient, "/api/v1/inventory/integration/production-completed");
            productionApp = CreateProductionApp(testEventBus);

            var mediatorProduction = productionApp.Services.GetRequiredService<IMediator>();
            var mediatorInventory = inventoryApp.Services.GetRequiredService<IMediator>();
            
            var fgMaterialId = "FG-101";
            var warehouseId = "WH-FG";
            var prdQuantity = 50m;
            
            // 2. Create and Release Production Order
            var prdId = await mediatorProduction.Send(new CreateProductionOrderCommand(
                fgMaterialId, "FG101", "Finished Good 101", prdQuantity
            ));
            await mediatorProduction.Send(new ReleaseProductionOrderCommand(prdId));

            // 3. Report Production -> Should trigger stock receipt via TestEventBus
            await mediatorProduction.Send(new ReportProductionCommand(
                prdId, prdQuantity, 0, warehouseId, "PRODUCTION_USER"
            ));

            // 4. Verify in Inventory
            await Task.Delay(500); 

            var stockInfo = await mediatorInventory.Send(new GetInventoryItemQuery(warehouseId, "DEFAULT_BIN", fgMaterialId));
            Assert.NotNull(stockInfo);
            Assert.Equal(prdQuantity, stockInfo.OnHandQuantity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACTUAL_ERROR: {ex.Message}");
            Console.WriteLine($"STACK_TRACE: {ex.StackTrace}");
            throw;
        }
        finally
        {
            inventoryApp?.Dispose();
            productionApp?.Dispose();
        }
    }
}
