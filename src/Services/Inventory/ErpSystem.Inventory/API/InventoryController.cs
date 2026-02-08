using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Inventory.Application;

namespace ErpSystem.Inventory.API;

[ApiController]
[Route("api/v1/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator) => _mediator = mediator;

    [HttpGet("items")]
    public async Task<IActionResult> Search([FromQuery] string? warehouseId, [FromQuery] string? binId, [FromQuery] string? materialCode, [FromQuery] int page = 1)
        => Ok(await _mediator.Send(new SearchInventoryItemsQuery(warehouseId, binId, materialCode, page)));

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromQuery] string warehouseId, [FromQuery] string binId, [FromQuery] string materialId)
        => Ok(await _mediator.Send(new GetInventoryItemQuery(warehouseId, binId, materialId)));

    [HttpPost("receive")]
    public async Task<IActionResult> Receive(ReceiveStockCommand command) => Ok(await _mediator.Send(command));

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(TransferStockCommand command) => Ok(await _mediator.Send(command));

    [HttpPost("issue")]
    public async Task<IActionResult> Issue(IssueStockCommand command) => Ok(await _mediator.Send(command));

    [HttpPost("reservations")]
    public async Task<IActionResult> Reserve(ReserveStockCommand command) => Ok(await _mediator.Send(command));

    [HttpPost("reservations/release")]
    public async Task<IActionResult> Release(ReleaseReservationCommand command) => Ok(await _mediator.Send(command));

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust(AdjustStockCommand command) => Ok(await _mediator.Send(command));

    [HttpGet("items/{id}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid id, [FromQuery] int page = 1)
        => Ok(await _mediator.Send(new GetStockTransactionsQuery(id, page)));
}
