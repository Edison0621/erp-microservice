using Microsoft.AspNetCore.Mvc;
using MediatR;
using ErpSystem.MasterData.Application;
using ErpSystem.MasterData.Infrastructure;

namespace ErpSystem.MasterData.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BomController(IMediator mediator, BomQueries queries) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BomReadModel>>> Get()
    {
        return await queries.GetAllBoMs();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BomReadModel>> Get(Guid id)
    {
        BomReadModel? bom = await queries.GetBomById(id);
        if (bom == null) return this.NotFound();
        return bom;
    }

    [HttpGet("material/{materialId}")]
    public async Task<ActionResult<List<BomReadModel>>> GetByMaterial(Guid materialId)
    {
        return await queries.GetBoMsByParentMaterial(materialId);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateBomRequest request)
    {
        Guid id = await mediator.Send(new CreateBomCommand(request));
        return this.CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{id}/components")]
    public async Task<ActionResult> AddComponent(Guid id, AddBomComponentCommand command)
    {
        if (id != command.BomId) return this.BadRequest();
        await mediator.Send(command);
        return this.Ok();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult> Activate(Guid id)
    {
        await mediator.Send(new ActivateBomCommand(id));
        return this.Ok();
    }
}
