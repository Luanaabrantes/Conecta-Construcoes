using System.ComponentModel.DataAnnotations;
using Conecta.Domain.Entidades;

namespace Conecta.Application.Categorias;

public record CategoriaEntrada([Required, StringLength(100)] string Nome, [Required, StringLength(100)] string Slug, [StringLength(5000)] string? Descricao, [StringLength(255)] string? IconeUrl);
public record AlterarAtivoEntrada([Required] bool? Ativo);
public record CategoriaDto(int Id, string Nome, string Slug, string? Descricao, string? IconeUrl,
    bool Ativo, DateTime CriadoEm, DateTime AtualizadoEm);
public record EspecialidadeDto(CategoriaDto Categoria, DateTime AdicionadoEm);

public sealed class ErroModulo(int status, string mensagem) : Exception(mensagem)
{
    public int Status { get; } = status;
}

public interface ICategoriaRepositorio
{
    Task<List<Categoria>> Listar(bool incluirInativas, CancellationToken ct);
    Task<Categoria?> PorSlug(string slug, CancellationToken ct);
    Task<Categoria?> PorId(int id, CancellationToken ct);
    Task Salvar(Categoria categoria, CancellationToken ct);
    Task<bool> PrestadorExiste(int id, CancellationToken ct);
    Task<List<PrestadorCategoria>> Especialidades(int prestadorId, CancellationToken ct);
    Task AdicionarEspecialidade(int prestadorId, int categoriaId, CancellationToken ct);
    Task RemoverEspecialidade(int prestadorId, int categoriaId, CancellationToken ct);
}