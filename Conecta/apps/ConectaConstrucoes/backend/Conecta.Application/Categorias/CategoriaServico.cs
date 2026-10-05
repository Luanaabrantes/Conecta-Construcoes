using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Conecta.Domain.Entidades;

namespace Conecta.Application.Categorias;

public sealed class CategoriaServico(ICategoriaRepositorio repositorio)
{
    public static CategoriaDto Dto(Categoria c) => new(c.Id, c.Nome, c.Slug, c.Descricao,
        c.IconeUrl, c.Ativo, c.CriadoEm, c.AtualizadoEm);

    public static string NormalizarSlug(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > 100)
            throw new ErroModulo(400, "Slug é obrigatório e deve ter até 100 caracteres.");
        var texto = new string(valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        texto = Regex.Replace(texto, @"\s+", "-");
        if (!Regex.IsMatch(texto, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || texto.Length > 100)
            throw new ErroModulo(400, "Slug deve conter letras ASCII, números e hífens entre palavras.");
        return texto;
    }

    private static void Preencher(Categoria c, CategoriaEntrada entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada.Nome) || entrada.Nome.Trim().Length > 100)
            throw new ErroModulo(400, "Nome é obrigatório e deve ter até 100 caracteres.");
        if (entrada.Descricao?.Length > 5000 || entrada.IconeUrl?.Length > 255)
            throw new ErroModulo(400, "Descrição deve ter até 5000 caracteres e ícone até 255.");
        if (!string.IsNullOrEmpty(entrada.IconeUrl) &&
            (!Uri.TryCreate(entrada.IconeUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            throw new ErroModulo(400, "Ícone deve ser uma URL HTTP ou HTTPS absoluta.");
        c.Nome = entrada.Nome.Trim();
        c.Slug = NormalizarSlug(entrada.Slug);
        c.Descricao = entrada.Descricao;
        c.IconeUrl = entrada.IconeUrl;
        c.AtualizadoEm = DateTime.UtcNow;
    }

    public async Task<List<CategoriaDto>> Listar(bool incluirInativas, CancellationToken ct) =>
        (await repositorio.Listar(incluirInativas, ct)).Select(Dto).ToList();

    public async Task<CategoriaDto> PorSlug(string slug, CancellationToken ct) =>
        Dto(await repositorio.PorSlug(NormalizarSlug(slug), ct) ?? throw new ErroModulo(404, "Categoria não encontrada."));

    public async Task<CategoriaDto> Criar(CategoriaEntrada entrada, CancellationToken ct)
    {
        var categoria = new Categoria();
        Preencher(categoria, entrada);
        await repositorio.Salvar(categoria, ct);
        return Dto(categoria);
    }

    public async Task<CategoriaDto> Editar(int id, CategoriaEntrada entrada, CancellationToken ct)
    {
        var categoria = await repositorio.PorId(id, ct) ?? throw new ErroModulo(404, "Categoria não encontrada.");
        Preencher(categoria, entrada);
        await repositorio.Salvar(categoria, ct);
        return Dto(categoria);
    }

    public async Task<CategoriaDto> AlterarAtivo(int id, bool ativo, CancellationToken ct)
    {
        var categoria = await repositorio.PorId(id, ct) ?? throw new ErroModulo(404, "Categoria não encontrada.");
        categoria.Ativo = ativo;
        categoria.AtualizadoEm = DateTime.UtcNow;
        await repositorio.Salvar(categoria, ct);
        return Dto(categoria);
    }

    private async Task ValidarPrestador(int id, CancellationToken ct)
    {
        if (!await repositorio.PrestadorExiste(id, ct)) throw new ErroModulo(404, "Prestador não encontrado.");
    }

    public async Task<List<EspecialidadeDto>> Especialidades(int id, CancellationToken ct)
    {
        await ValidarPrestador(id, ct);
        return (await repositorio.Especialidades(id, ct))
            .Select(v => new EspecialidadeDto(Dto(v.Categoria), v.AdicionadoEm)).ToList();
    }

    public async Task AdicionarEspecialidade(int id, int categoriaId, CancellationToken ct)
    {
        await ValidarPrestador(id, ct);
        await repositorio.AdicionarEspecialidade(id, categoriaId, ct);
    }

    public async Task RemoverEspecialidade(int id, int categoriaId, CancellationToken ct)
    {
        await ValidarPrestador(id, ct);
        await repositorio.RemoverEspecialidade(id, categoriaId, ct);
    }
}