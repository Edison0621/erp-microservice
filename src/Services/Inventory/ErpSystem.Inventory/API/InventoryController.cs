using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Inventory.Application;

namespace ErpSystem.Inventory.API;

[ApiController]
[Route("api/v1/inventory")]
public class InventoryController(IMediator mediator) : ControllerBase
{
    [HttpGet("items")]
    public async Task<IActionResult> Search([FromQuery] string? warehouseId, [FromQuery] string? binId, [FromQuery] string? materialCode, [FromQuery] int page = 1)
        =>
            this.Ok(await mediator.Send(new SearchInventoryItemsQuery(warehouseId, binId, materialCode, page)));

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromQuery] string warehouseId, [FromQuery] string binId, [FromQuery] string materialId)
        =>
            this.Ok(await mediator.Send(new GetInventoryItemQuery(warehouseId, binId, materialId)));

    [HttpPost("receive")]
    public async Task<IActionResult> Receive(ReceiveStockCommand command) => this.Ok(await mediator.Send(command));

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(TransferStockCommand command) => this.Ok(await mediator.Send(command));

    [HttpPost("issue")]
    public async Task<IActionResult> Issue(IssueStockCommand command) => this.Ok(await mediator.Send(command));

    [HttpPost("reservations")]
    public async Task<IActionResult> Reserve(ReserveStockCommand command) => this.Ok(await mediator.Send(command));

    [HttpPost("reservations/release")]
    public async Task<IActionResult> Release(ReleaseReservationCommand command) => this.Ok(await mediator.Send(command));

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust(AdjustStockCommand command) => this.Ok(await mediator.Send(command));

    [HttpGet("items/{id}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid id, [FromQuery] int page = 1)
        =>
            this.Ok(await mediator.Send(new GetStockTransactionsQuery(id, page)));
}
