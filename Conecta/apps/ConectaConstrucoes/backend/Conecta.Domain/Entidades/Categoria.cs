namespace Conecta.Domain.Entidades;

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? IconeUrl { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public ICollection<PrestadorCategoria> PrestadoresCategorias { get; set; }
        = new List<PrestadorCategoria>();
}