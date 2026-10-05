using Conecta.Application.Categorias;
using Conecta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Conecta.Infrastructure.Persistencia;

public sealed class CategoriaRepositorio(AppDbContext db) : ICategoriaRepositorio
{
    public Task<List<Categoria>> Listar(bool incluirInativas, CancellationToken ct) =>
        db.Categorias.AsNoTracking().Where(c => incluirInativas || c.Ativo).OrderBy(c => c.Nome).ToListAsync(ct);
    public Task<Categoria?> PorSlug(string slug, CancellationToken ct) =>
        db.Categorias.AsNoTracking().SingleOrDefaultAsync(c => c.Slug == slug && c.Ativo, ct);
    public Task<Categoria?> PorId(int id, CancellationToken ct) => db.Categorias.SingleOrDefaultAsync(c => c.Id == id, ct);
    public async Task Salvar(Categoria categoria, CancellationToken ct)
    {
        if (categoria.Id == 0) db.Categorias.Add(categoria);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "uq_categorias_slug" })
        { throw new ErroModulo(409, "Slug já utilizado."); }
    }
    public Task<bool> PrestadorExiste(int id, CancellationToken ct) => db.Prestadores.AnyAsync(p => p.UsuarioId == id, ct);
    public Task<List<PrestadorCategoria>> Especialidades(int id, CancellationToken ct) =>
        db.PrestadorCategorias.AsNoTracking().Include(v => v.Categoria).Where(v => v.PrestadorId == id)
            .OrderBy(v => v.Categoria.Nome).ToListAsync(ct);

    public async Task AdicionarEspecialidade(int prestadorId, int categoriaId, CancellationToken ct)
    {
        await using var transacao = await db.Database.BeginTransactionAsync(ct);
        // Serializa associação com ativação/desativação da mesma categoria.
        var categorias = await db.Categorias.FromSqlInterpolated(
            $"SELECT * FROM categorias WHERE id = {categoriaId} FOR UPDATE").ToListAsync(ct);
        var categoria = categorias.SingleOrDefault() ?? throw new ErroModulo(404, "Categoria não encontrada.");
        if (!categoria.Ativo) throw new ErroModulo(409, "Categoria inativa.");
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO prestador_categorias (prestador_id, categoria_id, adicionado_em) VALUES ({prestadorId}, {categoriaId}, {DateTime.UtcNow}) ON CONFLICT (prestador_id, categoria_id) DO NOTHING", ct);
            await transacao.CommitAsync(ct);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        { throw new ErroModulo(404, "Prestador ou categoria não encontrado."); }
    }
    public async Task RemoverEspecialidade(int prestadorId, int categoriaId, CancellationToken ct) =>
        await db.PrestadorCategorias.Where(v => v.PrestadorId == prestadorId && v.CategoriaId == categoriaId).ExecuteDeleteAsync(ct);
}