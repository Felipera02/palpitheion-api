namespace PalpitheionApi.Models;

public class Indicado
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public string? LinkImagemPequena { get; set; }
    public string? LinkImagemGrande { get; set; }

    public IEnumerable<Palpite> Palpites { get; set; } = [];
    public IEnumerable<Categoria> Categorias { get; set; } = [];

}
