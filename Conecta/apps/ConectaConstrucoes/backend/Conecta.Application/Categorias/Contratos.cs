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
    Task<List<Categoria>> Listar(bool incluirInativas, CancellationToken cancellationToken);
    Task<Categoria?> PorSlug(string slug, CancellationToken cancellationToken);
    Task<Categoria?> PorId(int categoriaId, CancellationToken cancellationToken);
    Task Salvar(Categoria categoria, CancellationToken cancellationToken);
    Task<bool> PrestadorExiste(int prestadorId, CancellationToken cancellationToken);
    Task<List<PrestadorCategoria>> Especialidades(int prestadorId, CancellationToken cancellationToken);
    Task AdicionarEspecialidade(int prestadorId, int categoriaId, CancellationToken cancellationToken);
    Task RemoverEspecialidade(int prestadorId, int categoriaId, CancellationToken cancellationToken);
}