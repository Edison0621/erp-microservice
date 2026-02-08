using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Procurement.Application;

namespace ErpSystem.Procurement.API;

[ApiController]
[Route("api/v1/procurement/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public PurchaseOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreatePOCommand command) => Ok(await _mediator.Send(command));

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? supplierId, [FromQuery] string? status, [FromQuery] int page = 1) 
        => Ok(await _mediator.Send(new SearchPOsQuery(supplierId, status, page)));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _mediator.Send(new GetPOByIdQuery(id)));

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(Guid id) => Ok(await _mediator.Send(new SubmitPOCommand(id)));

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveRequest request) 
        => Ok(await _mediator.Send(new ApprovePOCommand(id, request.ApprovedBy, request.Comment)));

    [HttpPost("{id}/send")]
    public async Task<IActionResult> Send(Guid id, [FromBody] SendRequest request) 
        => Ok(await _mediator.Send(new SendPOCommand(id, request.SentBy, request.Method)));

    [HttpPost("{id}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] string reason) 
        => Ok(await _mediator.Send(new ClosePOCommand(id, reason)));

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason) 
        => Ok(await _mediator.Send(new CancelPOCommand(id, reason)));

    [HttpGet("prices")]
    public async Task<IActionResult> GetPrices([FromQuery] string materialId, [FromQuery] string? supplierId) 
        => Ok(await _mediator.Send(new GetSupplierPriceHistoryQuery(materialId, supplierId)));
}

public record ApproveRequest(string ApprovedBy, string Comment);
public record SendRequest(string SentBy, string Method);

[ApiController]
[Route("api/v1/procurement/receipts")]
public class ReceiptsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReceiptsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> RecordReceipt(RecordReceiptCommand command) => Ok(await _mediator.Send(command));
}
