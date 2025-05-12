using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : Controller
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProductAsync(AddProductDTO product)
    {
        var result = await _dbService.AddProductAsync(product);
        return Ok(result);
    }
}