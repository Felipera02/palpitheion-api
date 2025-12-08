namespace PalpitheionApi.Models;

public class Categoria
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public string? Descricao { get; set; }

    public int? IndicadoVencedorId { get; set; }
    public Indicado? IndicadoVencedor { get; set; }

    public IEnumerable<Palpite> Palpites { get; set; } = [];
    public IEnumerable<Indicado> Indicados { get; set; } = [];
}
