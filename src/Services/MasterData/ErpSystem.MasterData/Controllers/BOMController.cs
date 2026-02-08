using Microsoft.AspNetCore.Mvc;
using MediatR;
using ErpSystem.MasterData.Application;
using ErpSystem.MasterData.Infrastructure;

namespace ErpSystem.MasterData.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BOMController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly BOMQueries _queries;

    public BOMController(IMediator mediator, BOMQueries queries)
    {
        _mediator = mediator;
        _queries = queries;
    }

    [HttpGet]
    public async Task<ActionResult<List<BOMReadModel>>> Get()
    {
        return await _queries.GetAllBOMs();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BOMReadModel>> Get(Guid id)
    {
        var bom = await _queries.GetBOMById(id);
        if (bom == null) return NotFound();
        return bom;
    }

    [HttpGet("material/{materialId}")]
    public async Task<ActionResult<List<BOMReadModel>>> GetByMaterial(Guid materialId)
    {
        return await _queries.GetBOMsByParentMaterial(materialId);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateBOMRequest request)
    {
        var id = await _mediator.Send(new CreateBOMCommand(request));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{id}/components")]
    public async Task<ActionResult> AddComponent(Guid id, AddBOMComponentCommand command)
    {
        if (id != command.BOMId) return BadRequest();
        await _mediator.Send(command);
        return Ok();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult> Activate(Guid id)
    {
        await _mediator.Send(new ActivateBOMCommand(id));
        return Ok();
    }
}
