using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.Procurement.Application;

namespace ErpSystem.Procurement.API;

[ApiController]
[Route("api/v1/procurement/purchase-orders")]
public class PurchaseOrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreatePoCommand command) => this.Ok(await mediator.Send(command));

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? supplierId, [FromQuery] string? status, [FromQuery] int page = 1)
        =>
            this.Ok(await mediator.Send(new SearchPOsQuery(supplierId, status, page)));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) => this.Ok(await mediator.Send(new GetPoByIdQuery(id)));

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(Guid id) => this.Ok(await mediator.Send(new SubmitPoCommand(id)));

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveRequest request)
        =>
            this.Ok(await mediator.Send(new ApprovePoCommand(id, request.ApprovedBy, request.Comment)));

    [HttpPost("{id}/send")]
    public async Task<IActionResult> Send(Guid id, [FromBody] SendRequest request)
        =>
            this.Ok(await mediator.Send(new SendPoCommand(id, request.SentBy, request.Method)));

    [HttpPost("{id}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] string reason)
        =>
            this.Ok(await mediator.Send(new ClosePoCommand(id, reason)));

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason)
        =>
            this.Ok(await mediator.Send(new CancelPoCommand(id, reason)));

    [HttpPost("{id}/return")]
    public async Task<IActionResult> ReturnGoods(Guid id, [FromBody] ReturnGoodsRequest request)
        =>
            this.Ok(await mediator.Send(new ReturnGoodsCommand(id, request.Lines, request.Reason)));

    [HttpGet("prices")]
    public async Task<IActionResult> GetPrices([FromQuery] string materialId, [FromQuery] string? supplierId)
        =>
            this.Ok(await mediator.Send(new GetSupplierPriceHistoryQuery(materialId, supplierId)));
}

public record ApproveRequest(string ApprovedBy, string Comment);

public record SendRequest(string SentBy, string Method);

public record ReturnGoodsRequest(List<Domain.ReturnLine> Lines, string Reason);

[ApiController]
[Route("api/v1/procurement/receipts")]
public class ReceiptsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RecordReceipt(RecordReceiptCommand command) => this.Ok(await mediator.Send(command));
}
