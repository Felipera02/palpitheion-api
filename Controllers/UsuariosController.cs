using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PalpitheionApi.Data;
using PalpitheionApi.Services;

namespace PalpitheionApi.Controllers;

[ApiController]
[Route("usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly PalpitheionContext _ctx;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly StatusPalpitesService _statusPalpites;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        PalpitheionContext ctx,
        UserManager<IdentityUser> userManager,
        StatusPalpitesService statusPalpites,
        ILogger<UsuariosController> logger)
    {
        _ctx = ctx;
        _userManager = userManager;
        _statusPalpites = statusPalpites;
        _logger = logger;
    }

    // GET usuarios/   (disponível apenas quando status singleton está BLOQUEADO)
    [HttpGet]
    public async Task<IActionResult> ObterTodos()
    {
        if (!_statusPalpites.GetStatus())
            return Forbid();

        // calcula pontuações (palpites certos): join palpites -> categorias onde indicado vencedor == palpite.IndicadoId
        var scoresList = await (from p in _ctx.Palpites
                                join c in _ctx.Categorias on p.CategoriaId equals c.Id
                                where c.IndicadoVencedorId != null && p.IndicadoId == c.IndicadoVencedorId
                                group p by p.IdentityUserId into g
                                select new { UserId = g.Key, Score = g.Count() })
                                .ToListAsync();

        var scores = scoresList.ToDictionary(x => x.UserId, x => x.Score);

        var users = await _userManager.Users.ToListAsync();

        var result = users.Select(u => new
        {
            Username = u.UserName,
            Pontuacao = scores.TryGetValue(u.Id, out var s) ? s : 0
        });

        return Ok(result);
    }

    // GET usuarios/minha-pontuacao
    [HttpGet("minha-pontuacao")]
    [Authorize]
    public async Task<IActionResult> MinhaPontuacao()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var score = await (from p in _ctx.Palpites
                           join c in _ctx.Categorias on p.CategoriaId equals c.Id
                           where p.IdentityUserId == userId && c.IndicadoVencedorId != null && p.IndicadoId == c.IndicadoVencedorId
                           select p).CountAsync();

        return Ok(new { Username = user.UserName, Pontuacao = score });
    }
}