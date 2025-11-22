using Microsoft.AspNetCore.Mvc;

namespace Personelim.Controllers;

[ApiController]
[Route("api/[controller]")]

public class WakeUpController : ControllerBase
{
    [HttpGet]
    public IActionResult WakeUp()
    {
        return Ok();
    }
}