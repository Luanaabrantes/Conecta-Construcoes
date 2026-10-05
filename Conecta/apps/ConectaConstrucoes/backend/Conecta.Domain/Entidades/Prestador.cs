namespace Conecta.Domain.Entidades;

public class Prestador
{
    public int UsuarioId { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string PlanoAssinatura { get; set; } = "G";
    public string StatusDisponibilidade { get; set; } = "DISPONIVEL";
    public Usuario Usuario { get; set; } = null!;
    public ICollection<PrestadorCategoria> Categorias { get; set; } = new List<PrestadorCategoria>();
}