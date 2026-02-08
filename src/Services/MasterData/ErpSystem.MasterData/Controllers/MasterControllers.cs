using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.MasterData.Application;
using ErpSystem.MasterData.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ErpSystem.MasterData.Domain;

namespace ErpSystem.MasterData.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class MaterialsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly MasterDataReadDbContext _readContext;

    public MaterialsController(IMediator mediator, MasterDataReadDbContext readContext)
    {
        _mediator = mediator;
        _readContext = readContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMaterialRequest request)
    {
        var id = await _mediator.Send(new CreateMaterialCommand(request));
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MaterialReadModel>>> GetAll() => await _readContext.Materials.ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<MaterialReadModel>> GetById(Guid id)
    {
        var m = await _readContext.Materials.FindAsync(id);
        return m != null ? m : NotFound();
    }

    [HttpPut("{id}/info")]
    public async Task<IActionResult> UpdateInfo(Guid id, UpdateMaterialInfoCommand command)
    {
        if (id != command.MaterialId) return BadRequest();
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPut("{id}/attributes")]
    public async Task<IActionResult> UpdateAttributes(Guid id, UpdateMaterialAttributesCommand command)
    {
        if (id != command.MaterialId) return BadRequest();
        await _mediator.Send(command);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/[controller]")]
public class PartnersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly MasterDataReadDbContext _readContext;

    public PartnersController(IMediator mediator, MasterDataReadDbContext readContext)
    {
        _mediator = mediator;
        _readContext = readContext;
    }

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier(CreateSupplierRequest request)
    {
        var id = await _mediator.Send(new CreateSupplierCommand(request));
        return Ok(id);
    }

    [HttpPut("suppliers/{id}/profile")]
    public async Task<IActionResult> UpdateSupplierProfile(Guid id, UpdateSupplierProfileCommand command)
    {
        if (id != command.SupplierId) return BadRequest();
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer(CreateCustomerRequest request)
    {
        var id = await _mediator.Send(new CreateCustomerCommand(request));
        return Ok(id);
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult<IEnumerable<SupplierReadModel>>> GetSuppliers() => await _readContext.Suppliers.ToListAsync();

    [HttpGet("customers")]
    public async Task<ActionResult<IEnumerable<CustomerReadModel>>> GetCustomers() => await _readContext.Customers.ToListAsync();
}
