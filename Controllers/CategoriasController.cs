using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PalpitheionApi.Data;
using PalpitheionApi.Models;
using PalpitheionApi.Services;
using System.Security.Claims;

namespace PalpitheionApi.Controllers;

[ApiController]
[Route("categorias")]
public class CategoriasController : ControllerBase
{
    private readonly PalpitheionContext _ctx;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly StatusPalpitesService _statusPalpites;
    private readonly ILogger<CategoriasController> _logger;

    public CategoriasController(
        PalpitheionContext ctx,
        UserManager<IdentityUser> userManager,
        StatusPalpitesService statusPalpites,
        ILogger<CategoriasController> logger)
    {
        _ctx = ctx;
        _userManager = userManager;
        _statusPalpites = statusPalpites;
        _logger = logger;
    }

    // DTOs
    public record PalpiteDto(int Id, int IndicadoId, string IdentityUserId);
    public record IndicadoDto(int Id, string Nome, string? LinkImagemPequena, string? LinkImagemGrande);
    public record CategoriaDto(
        int Id,
        string Nome,
        string? Descricao,
        int? IndicadoVencedorId,
        IndicadoDto? IndicadoVencedor,
        IEnumerable<IndicadoDto> Indicados,
        PalpiteDto? MeuPalpite);

    // GET categorias/meus-palpites
    [HttpGet("meus-palpites")]
    [Authorize]
    public async Task<IActionResult> MeusPalpites()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var categorias = await _ctx.Categorias
            .Include(c => c.Indicados)
            .Include(c => c.IndicadoVencedor)
            .ToListAsync();

        var palpites = await _ctx.Palpites
            .Where(p => p.IdentityUserId == userId)
            .ToListAsync();

        var result = categorias.Select(c =>
        {
            var palpite = palpites.FirstOrDefault(p => p.CategoriaId == c.Id);
            return new CategoriaDto(
                c.Id,
                c.Nome,
                c.Descricao,
                c.IndicadoVencedorId,
                c.IndicadoVencedor is not null ? new IndicadoDto(c.IndicadoVencedor.Id, c.IndicadoVencedor.Nome, c.IndicadoVencedor.LinkImagemPequena, c.IndicadoVencedor.LinkImagemGrande) : null,
                c.Indicados.Select(i => new IndicadoDto(i.Id, i.Nome, i.LinkImagemPequena, i.LinkImagemGrande)),
                palpite is null ? null : new PalpiteDto(palpite.Id, palpite.IndicadoId, palpite.IdentityUserId)
            );
        });

        return Ok(result);
    }

    // GET categorias/{idCategoria}/meu-palpite
    [HttpGet("{idCategoria:int}/meu-palpite")]
    [Authorize]
    public async Task<IActionResult> MeuPalpitePorCategoria([FromRoute] int idCategoria)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var categoria = await _ctx.Categorias
            .Include(c => c.Indicados)
            .Include(c => c.IndicadoVencedor)
            .FirstOrDefaultAsync(c => c.Id == idCategoria);

        if (categoria == null) return NotFound();

        var palpite = await _ctx.Palpites
            .Where(p => p.CategoriaId == idCategoria && p.IdentityUserId == userId)
            .FirstOrDefaultAsync();

        var result = new CategoriaDto(
            categoria.Id,
            categoria.Nome,
            categoria.Descricao,
            categoria.IndicadoVencedorId,
            categoria.IndicadoVencedor is not null ? new IndicadoDto(categoria.IndicadoVencedor.Id, categoria.IndicadoVencedor.Nome, categoria.IndicadoVencedor.LinkImagemPequena, categoria.IndicadoVencedor.LinkImagemGrande) : null,
            categoria.Indicados.Select(i => new IndicadoDto(i.Id, i.Nome, i.LinkImagemPequena, i.LinkImagemGrande)),
            palpite is null ? null : new PalpiteDto(palpite.Id, palpite.IndicadoId, palpite.IdentityUserId)
        );

        return Ok(result);
    }

    // GET categorias/palpites/usuario/{userName}
    // Acesso permitido apenas quando StatusPalpitesService.GetStatus() == true
    [HttpGet("palpites/usuario/{userName}")]
    public async Task<IActionResult> PalpitesPorUsuario([FromRoute] string userName)
    {
        if (!_statusPalpites.GetStatus())
            return Forbid();

        var user = await _userManager.FindByNameAsync(userName);
        if (user == null) return NotFound("usuario não encontrado.");

        var categorias = await _ctx.Categorias
            .Include(c => c.Indicados)
            .Include(c => c.IndicadoVencedor)
            .ToListAsync();

        var palpites = await _ctx.Palpites
            .Where(p => p.IdentityUserId == user.Id)
            .ToListAsync();

        var result = categorias.Select(c =>
        {
            var palpite = palpites.FirstOrDefault(p => p.CategoriaId == c.Id);
            return new CategoriaDto(
                c.Id,
                c.Nome,
                c.Descricao,
                c.IndicadoVencedorId,
                c.IndicadoVencedor is not null ? new IndicadoDto(c.IndicadoVencedor.Id, c.IndicadoVencedor.Nome, c.IndicadoVencedor.LinkImagemPequena, c.IndicadoVencedor.LinkImagemGrande) : null,
                c.Indicados.Select(i => new IndicadoDto(i.Id, i.Nome, i.LinkImagemPequena, i.LinkImagemGrande)),
                palpite is null ? null : new PalpiteDto(palpite.Id, palpite.IndicadoId, palpite.IdentityUserId)
            );
        });

        return Ok(result);
    }

    // POST categorias/ (cria nova categoria) - Admin only
    public record CategoriaCreateRequest(string Nome, string? Descricao);
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CriarCategoria([FromBody] CategoriaCreateRequest model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Nome))
            return BadRequest("Nome é obrigatório.");

        var categoria = new Categoria
        {
            Nome = model.Nome,
            Descricao = model.Descricao
        };

        _ctx.Categorias.Add(categoria);
        await _ctx.SaveChangesAsync();

        return CreatedAtAction(nameof(Detalhes), new { categoriaId = categoria.Id }, categoria);
    }

    // GET categorias/indicados (obtem todas as categorias incluindo seus candidatos)
    [HttpGet("indicados")]
    public async Task<IActionResult> TodasComIndicados()
    {
        var categorias = await _ctx.Categorias
            .Include(c => c.Indicados)
            .Include(c => c.IndicadoVencedor)
            .ToListAsync();

        var result = categorias.Select(c => new
        {
            c.Id,
            c.Nome,
            c.Descricao,
            IndicadoVencedor = c.IndicadoVencedor is not null ? new { c.IndicadoVencedor.Id, c.IndicadoVencedor.Nome } : null,
            Indicados = c.Indicados.Select(i => new { i.Id, i.Nome, i.LinkImagemPequena, i.LinkImagemGrande })
        });

        return Ok(result);
    }

    // PUT categorias/ (edita nome e descrição) - Admin only
    public record CategoriaEditRequest(int Id, string Nome, string? Descricao);
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditarCategoria([FromBody] CategoriaEditRequest model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Nome))
            return BadRequest("Modelo inválido.");

        var categoria = await _ctx.Categorias.FindAsync(model.Id);
        if (categoria == null) return NotFound();

        categoria.Nome = model.Nome;
        categoria.Descricao = model.Descricao;

        _ctx.Categorias.Update(categoria);
        await _ctx.SaveChangesAsync();

        return NoContent();
    }

    // DELETE categorias/{categoriaId} - Admin only
    [HttpDelete("{categoriaId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExcluirCategoria([FromRoute] int categoriaId)
    {
        var categoria = await _ctx.Categorias
            .Include(c => c.Indicados)
            .FirstOrDefaultAsync(c => c.Id == categoriaId);

        if (categoria == null) return NotFound();

        // Remove palpites relacionados
        var palpites = _ctx.Palpites.Where(p => p.CategoriaId == categoriaId);
        _ctx.Palpites.RemoveRange(palpites);

        // Remove ligação many-to-many (EF Core gerencia a tabela de junção)
        categoria.Indicados = new List<Indicado>();

        _ctx.Categorias.Remove(categoria);
        await _ctx.SaveChangesAsync();

        return NoContent();
    }

    // POST categorias/{categoriaId}/indicado/{indicadoId} - adicionar indicado na categoria (Admin only)
    [HttpPost("{categoriaId:int}/indicado/{indicadoId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdicionarIndicado([FromRoute] int categoriaId, [FromRoute] int indicadoId)
    {
        var categoria = await _ctx.Categorias
            .Include(c => c.Indicados)
            .FirstOrDefaultAsync(c => c.Id == categoriaId);
        if (categoria == null) return NotFound("Categoria não encontrada.");

        var indicado = await _ctx.Indicados.FindAsync(indicadoId);
        if (indicado == null) return NotFound("Indicado não encontrado.");

        if (!categoria.Indicados.Any(i => i.Id == indicadoId))
        {
            // EF Core many-to-many: adicionar à coleção
            var indicadores = categoria.Indicados.ToList();
            indicadores.Add(indicado);
            categoria.Indicados = indicadores;
            _ctx.Categorias.Update(categoria);
            await _ctx.SaveChangesAsync();
        }

        return NoContent();
    }

    // DELETE categorias/indicado/{indicadoId} - remove indicado da(s) categoria(s) e opcionalmente exclui o indicado (Admin only)
    [HttpDelete("indicado/{indicadoId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoverIndicado([FromRoute] int indicadoId)
    {
        var indicado = await _ctx.Indicados
            .Include(i => i.Categorias)
            .FirstOrDefaultAsync(i => i.Id == indicadoId);

        if (indicado == null) return NotFound();

        // Para cada categoria que referencia esse indicado, remova a associação e, se for o vencedor, zere.
        var categoriaIds = indicado.Categorias?.Select(c => c.Id).ToList() ?? new List<int>();
        foreach (var catId in categoriaIds)
        {
            var categoria = await _ctx.Categorias
                .Include(c => c.Indicados)
                .FirstOrDefaultAsync(c => c.Id == catId);

            if (categoria == null) continue;

            if (categoria.Indicados.Any(i => i.Id == indicadoId))
            {
                categoria.Indicados = categoria.Indicados.Where(i => i.Id != indicadoId).ToList();
            }

            if (categoria.IndicadoVencedorId == indicadoId)
            {
                categoria.IndicadoVencedorId = null;
            }

            _ctx.Categorias.Update(categoria);
        }

        indicado.Categorias = new List<Categoria>();
        _ctx.Indicados.Update(indicado);

        await _ctx.SaveChangesAsync();

        return NoContent();
    }


    // POST categorias/{categoriaId}/indicado-vencedor/{indicadoId?} - define vencedor da categoria (Admin only)
    [HttpPost("{categoriaId:int}/indicado-vencedor/{indicadoId?}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DefinirIndicadoVencedor([FromRoute] int categoriaId, [FromRoute] int? indicadoId)
    {
        var categoria = await _ctx.Categorias
            .Include(c => c.Indicados)
            .FirstOrDefaultAsync(c => c.Id == categoriaId);

        if (categoria == null) return NotFound("Categoria não encontrada.");

        if (indicadoId is null)
        {
            categoria.IndicadoVencedorId = null;
            _ctx.Categorias.Update(categoria);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        var indicado = categoria.Indicados.FirstOrDefault(i => i.Id == indicadoId);
        if (indicado == null) return BadRequest("Indicado não pertence à categoria.");

        // Define vencedor para a categoria
        categoria.IndicadoVencedorId = indicadoId;
        _ctx.Categorias.Update(categoria);
        await _ctx.SaveChangesAsync();

        return NoContent();
    }

    // Helper: detalhes (usado no CreatedAtAction)
    [HttpGet("{categoriaId:int}")]
    public async Task<IActionResult> Detalhes([FromRoute] int categoriaId)
    {
        var categoria = await _ctx.Categorias
            .Include(c => c.Indicados)
            .Include(c => c.IndicadoVencedor)
            .FirstOrDefaultAsync(c => c.Id == categoriaId);

        if (categoria == null) return NotFound();

        var dto = new
        {
            categoria.Id,
            categoria.Nome,
            categoria.Descricao,
            IndicadoVencedor = categoria.IndicadoVencedor is not null
                ? new { categoria.IndicadoVencedor.Id, categoria.IndicadoVencedor.Nome, categoria.IndicadoVencedor.LinkImagemPequena, categoria.IndicadoVencedor.LinkImagemGrande }
                : null,
            Indicados = categoria.Indicados.Select(i => new { i.Id, i.Nome, i.LinkImagemPequena, i.LinkImagemGrande })
        };

        return Ok(dto);
    }
}