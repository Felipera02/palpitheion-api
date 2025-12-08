using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalpitheionApi.Services;

namespace PalpitheionApi.Controllers;

[ApiController]
[Route("status-palpite")]
public class StatusPalpitesController : ControllerBase
{
    private readonly StatusPalpitesService _statusPalpites;
    private readonly ILogger<StatusPalpitesController> _logger;

    public StatusPalpitesController(StatusPalpitesService statusPalpites, ILogger<StatusPalpitesController> logger)
    {
        _statusPalpites = statusPalpites;
        _logger = logger;
    }

    // GET status-palpite/
    [HttpGet]
    public IActionResult GetStatus()
    {
        var bloqueado = _statusPalpites.GetStatus();
        return Ok(new { bloqueado });
    }

    // POST status-palpite/alterar  (somente Admin)
    [HttpPost("alterar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Alterar()
    {
        var novoStatus = await _statusPalpites.ToggleStatus();
        _logger.LogInformation("StatusPalpites alterado para {novoStatus} por {user}", novoStatus, User?.Identity?.Name);
        return Ok(new { bloqueado = novoStatus });
    }
}