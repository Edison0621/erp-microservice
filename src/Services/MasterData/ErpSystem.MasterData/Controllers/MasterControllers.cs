using MediatR;
using Microsoft.AspNetCore.Mvc;
using ErpSystem.MasterData.Application;
using ErpSystem.MasterData.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.MasterData.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class MaterialsController(IMediator mediator, MasterDataReadDbContext readContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateMaterialRequest request)
    {
        Guid id = await mediator.Send(new CreateMaterialCommand(request));
        return this.CreatedAtAction(nameof(this.GetById), new { id }, id);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MaterialReadModel>>> GetAll() => await readContext.Materials.ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<MaterialReadModel>> GetById(Guid id)
    {
        MaterialReadModel? m = await readContext.Materials.FindAsync(id);
        return m != null ? m : this.NotFound();
    }

    [HttpPut("{id}/info")]
    public async Task<IActionResult> UpdateInfo(Guid id, UpdateMaterialInfoCommand command)
    {
        if (id != command.MaterialId) return this.BadRequest();
        await mediator.Send(command);
        return this.NoContent();
    }

    [HttpPut("{id}/attributes")]
    public async Task<IActionResult> UpdateAttributes(Guid id, UpdateMaterialAttributesCommand command)
    {
        if (id != command.MaterialId) return this.BadRequest();
        await mediator.Send(command);
        return this.NoContent();
    }
}

[ApiController]
[Route("api/v1/[controller]")]
public class PartnersController(IMediator mediator, MasterDataReadDbContext readContext) : ControllerBase
{
    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier(CreateSupplierRequest request)
    {
        Guid id = await mediator.Send(new CreateSupplierCommand(request));
        return this.Ok(id);
    }

    [HttpPut("suppliers/{id}/profile")]
    public async Task<IActionResult> UpdateSupplierProfile(Guid id, UpdateSupplierProfileCommand command)
    {
        if (id != command.SupplierId) return this.BadRequest();
        await mediator.Send(command);
        return this.NoContent();
    }

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer(CreateCustomerRequest request)
    {
        Guid id = await mediator.Send(new CreateCustomerCommand(request));
        return this.Ok(id);
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult<IEnumerable<SupplierReadModel>>> GetSuppliers() => await readContext.Suppliers.ToListAsync();

    [HttpGet("customers")]
    public async Task<ActionResult<IEnumerable<CustomerReadModel>>> GetCustomers() => await readContext.Customers.ToListAsync();
}
