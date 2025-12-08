using Microsoft.AspNetCore.Identity;

namespace PalpitheionApi.Models;

public class Palpite
{
    public int Id { get; set; }

    public required string IdentityUserId { get; set; }
    public IdentityUser? IdentityUser { get; set; }

    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public int IndicadoId { get; set; }
    public Indicado? Indicado { get; set; }
}
