using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PalpitheionApi.Data;
using PalpitheionApi.Models;

namespace PalpitheionApi.Controllers;

[ApiController]
[Route("indicados")]
public class IndicadosControllers : ControllerBase
{
    private readonly PalpitheionContext _ctx;
    private readonly ILogger<IndicadosControllers> _logger;

    public IndicadosControllers(PalpitheionContext ctx, ILogger<IndicadosControllers> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public record IndicadoDto(int Id, string Nome, string? LinkImagemPequena, string? LinkImagemGrande, IEnumerable<int> CategoriaIds);
    public record IndicadoCreateRequest(string Nome, string? LinkImagemPequena, string? LinkImagemGrande);
    public record IndicadoEditRequest(int Id, string Nome, string? LinkImagemPequena, string? LinkImagemGrande);

    // POST indicados/  (somente Admin)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CriarIndicado([FromBody] IndicadoCreateRequest model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Nome))
            return BadRequest("Nome é obrigatório.");

        var indicado = new Indicado
        {
            Nome = model.Nome,
            LinkImagemPequena = model.LinkImagemPequena,
            LinkImagemGrande = model.LinkImagemGrande
        };

        _ctx.Indicados.Add(indicado);
        await _ctx.SaveChangesAsync();

        var dto = new IndicadoDto(indicado.Id, indicado.Nome, indicado.LinkImagemPequena, indicado.LinkImagemGrande, Enumerable.Empty<int>());
        return CreatedAtAction(nameof(ObterPorId), new { id = indicado.Id }, dto);
    }

    // GET indicados/
    [HttpGet]
    public async Task<IActionResult> ObterTodos()
    {
        var indicados = await _ctx.Indicados
            .Include(i => i.Categorias)
            .ToListAsync();

        var result = indicados.Select(i => new IndicadoDto(
            i.Id,
            i.Nome,
            i.LinkImagemPequena,
            i.LinkImagemGrande,
            i.Categorias?.Select(c => c.Id) ?? Enumerable.Empty<int>()
        ));

        return Ok(result);
    }

    // PUT indicados/  (somente Admin)
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditarIndicado([FromBody] IndicadoEditRequest model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Nome))
            return BadRequest("Modelo inválido.");

        var indicado = await _ctx.Indicados.FindAsync(model.Id);
        if (indicado == null) return NotFound();

        indicado.Nome = model.Nome;
        indicado.LinkImagemPequena = model.LinkImagemPequena;
        indicado.LinkImagemGrande = model.LinkImagemGrande;

        _ctx.Indicados.Update(indicado);
        await _ctx.SaveChangesAsync();

        return NoContent();
    }

    // DELETE indicados/{indicadoId}  (somente Admin)
    [HttpDelete("{indicadoId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExcluirIndicado([FromRoute] int indicadoId)
    {
        var indicado = await _ctx.Indicados
            .Include(i => i.Categorias)
            .FirstOrDefaultAsync(i => i.Id == indicadoId);

        if (indicado == null) return NotFound();

        // Remove palpites associados ao indicado
        var palpites = _ctx.Palpites.Where(p => p.IndicadoId == indicadoId);
        _ctx.Palpites.RemoveRange(palpites);

        // Desvincula das categorias (EF Core vai remover entradas da tabela de junção ao salvar)
        indicado.Categorias = new List<Categoria>();
        _ctx.Indicados.Remove(indicado);

        await _ctx.SaveChangesAsync();

        return NoContent();
    }

    // GET indicados/{id} - helper para CreatedAtAction
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObterPorId([FromRoute] int id)
    {
        var indicado = await _ctx.Indicados
            .Include(i => i.Categorias)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (indicado == null) return NotFound();

        var dto = new IndicadoDto(
            indicado.Id,
            indicado.Nome,
            indicado.LinkImagemPequena,
            indicado.LinkImagemGrande,
            indicado.Categorias?.Select(c => c.Id) ?? Enumerable.Empty<int>());

        return Ok(dto);
    }
}