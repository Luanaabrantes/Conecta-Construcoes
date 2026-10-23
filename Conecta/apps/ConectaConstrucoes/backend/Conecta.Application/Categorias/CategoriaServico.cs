using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Conecta.Domain.Entidades;

namespace Conecta.Application.Categorias;

public sealed class CategoriaServico(ICategoriaRepositorio categoriaRepositorio)
{
    public static CategoriaDto Dto(Categoria categoria) => new(categoria.Id, categoria.Nome, categoria.Slug, categoria.Descricao,
        categoria.IconeUrl, categoria.Ativo, categoria.CriadoEm, categoria.AtualizadoEm);

    public static string NormalizarSlug(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > 100)
            throw new ErroModulo(400, "Slug é obrigatório e deve ter até 100 caracteres.");
        var texto = new string(valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD)
            .Where(caractere => CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark).ToArray());
        texto = Regex.Replace(texto, @"\s+", "-");
        if (!Regex.IsMatch(texto, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || texto.Length > 100)
            throw new ErroModulo(400, "Slug deve conter letras ASCII, números e hífens entre palavras.");
        return texto;
    }

    private static void Preencher(Categoria categoria, CategoriaEntrada entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada.Nome) || entrada.Nome.Trim().Length > 100)
            throw new ErroModulo(400, "Nome é obrigatório e deve ter até 100 caracteres.");
        if (entrada.Descricao?.Length > 5000 || entrada.IconeUrl?.Length > 255)
            throw new ErroModulo(400, "Descrição deve ter até 5000 caracteres e ícone até 255.");
        if (!string.IsNullOrEmpty(entrada.IconeUrl) &&
            (!Uri.TryCreate(entrada.IconeUrl, UriKind.Absolute, out var enderecoIcone) ||
             (enderecoIcone.Scheme != Uri.UriSchemeHttp && enderecoIcone.Scheme != Uri.UriSchemeHttps)))
            throw new ErroModulo(400, "Ícone deve ser uma URL HTTP ou HTTPS absoluta.");
        categoria.Nome = entrada.Nome.Trim();
        categoria.Slug = NormalizarSlug(entrada.Slug);
        categoria.Descricao = entrada.Descricao;
        categoria.IconeUrl = entrada.IconeUrl;
    }

    public async Task<List<CategoriaDto>> Listar(bool incluirInativas, CancellationToken cancellationToken) =>
        (await categoriaRepositorio.Listar(incluirInativas, cancellationToken)).Select(Dto).ToList();

    public async Task<CategoriaDto> PorSlug(string slug, CancellationToken cancellationToken) =>
        Dto(await categoriaRepositorio.PorSlug(NormalizarSlug(slug), cancellationToken) ?? throw new ErroModulo(404, "Categoria não encontrada."));

    public async Task<CategoriaDto> Criar(CategoriaEntrada entrada, CancellationToken cancellationToken)
    {
        var categoria = new Categoria();
        Preencher(categoria, entrada);
        await categoriaRepositorio.Salvar(categoria, cancellationToken);
        return Dto(categoria);
    }

    public async Task<CategoriaDto> Editar(int categoriaId, CategoriaEntrada entrada, CancellationToken cancellationToken)
    {
        var categoria = await categoriaRepositorio.PorId(categoriaId, cancellationToken) ?? throw new ErroModulo(404, "Categoria não encontrada.");
        Preencher(categoria, entrada);
        // O default do PostgreSQL vale para INSERT; categorias não possui trigger de UPDATE.
        categoria.AtualizadoEm = DateTime.UtcNow;
        await categoriaRepositorio.Salvar(categoria, cancellationToken);
        return Dto(categoria);
    }

    public async Task<CategoriaDto> AlterarAtivo(int categoriaId, bool ativo, CancellationToken cancellationToken)
    {
        var categoria = await categoriaRepositorio.PorId(categoriaId, cancellationToken) ?? throw new ErroModulo(404, "Categoria não encontrada.");
        categoria.Ativo = ativo;
        categoria.AtualizadoEm = DateTime.UtcNow;
        await categoriaRepositorio.Salvar(categoria, cancellationToken);
        return Dto(categoria);
    }

    private async Task ValidarPrestador(int prestadorId, CancellationToken cancellationToken)
    {
        if (!await categoriaRepositorio.PrestadorExiste(prestadorId, cancellationToken)) throw new ErroModulo(404, "Prestador não encontrado.");
    }

    public async Task<List<EspecialidadeDto>> Especialidades(int prestadorId, CancellationToken cancellationToken)
    {
        await ValidarPrestador(prestadorId, cancellationToken);
        return (await categoriaRepositorio.Especialidades(prestadorId, cancellationToken))
            .Select(especialidade => new EspecialidadeDto(Dto(especialidade.Categoria), especialidade.AdicionadoEm)).ToList();
    }

    public async Task AdicionarEspecialidade(int prestadorId, int categoriaId, CancellationToken cancellationToken)
    {
        await ValidarPrestador(prestadorId, cancellationToken);
        await categoriaRepositorio.AdicionarEspecialidade(prestadorId, categoriaId, cancellationToken);
    }

    public async Task RemoverEspecialidade(int prestadorId, int categoriaId, CancellationToken cancellationToken)
    {
        await ValidarPrestador(prestadorId, cancellationToken);
        await categoriaRepositorio.RemoverEspecialidade(prestadorId, categoriaId, cancellationToken);
    }
}