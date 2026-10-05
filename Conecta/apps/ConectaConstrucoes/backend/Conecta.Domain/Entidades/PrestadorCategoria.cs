namespace Conecta.Domain.Entidades;

public class PrestadorCategoria
{
    public int PrestadorId { get; set; }
    public int CategoriaId { get; set; }
    public DateTime AdicionadoEm { get; set; } = DateTime.UtcNow;

    public Prestador Prestador { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
}