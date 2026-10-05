namespace Conecta.Domain.Entidades;

// Suporte mínimo, fiel a objs/schema.sql; cadastro/login pertence à autenticação.
public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public bool? Ativo { get; set; } = true;
    public string? Documento { get; set; }
    public bool? Verificado { get; set; } = false;
    public string? FotoPerfilUrl { get; set; }
    public decimal? MediaAvaliacao { get; set; } = 0;
    public int? TotalAvaliacoes { get; set; } = 0;
    public DateTime? TermosAceitosEm { get; set; }
    public DateTime? DataCriacao { get; set; } = DateTime.UtcNow;
}