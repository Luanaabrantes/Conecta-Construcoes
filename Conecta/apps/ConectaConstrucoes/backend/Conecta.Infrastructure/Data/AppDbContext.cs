using Conecta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Conecta.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<PrestadorCategoria> PrestadorCategorias => Set<PrestadorCategoria>();
    public DbSet<Prestador> Prestadores => Set<Prestador>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);
        m.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        var u = m.Entity<Usuario>();
        u.ToTable("usuarios");
        u.HasKey(x => x.Id);
        u.Property(x => x.Email).HasMaxLength(255).IsRequired();
        u.HasIndex(x => x.Email).IsUnique();
        u.Property(x => x.SenhaHash).HasMaxLength(255).IsRequired();
        u.Property(x => x.Perfil).HasMaxLength(20).IsRequired();
        u.Property(x => x.Ativo).HasDefaultValue(true);
        u.Property(x => x.Documento).HasMaxLength(20);
        u.Property(x => x.Verificado).HasDefaultValue(false);
        u.Property(x => x.FotoPerfilUrl).HasMaxLength(255);
        u.Property(x => x.MediaAvaliacao).HasPrecision(3, 2).HasDefaultValue(0m);
        u.Property(x => x.TotalAvaliacoes).HasDefaultValue(0);
        u.Property(x => x.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
        // V1/V2 da equipe usam timestamp sem fuso nestes campos de suporte.
        var utcLegado = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            valor => DateTime.SpecifyKind(valor.ToUniversalTime(), DateTimeKind.Unspecified),
            valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));
        u.Property(x => x.DataCriacao).HasColumnType("timestamp without time zone").HasConversion(utcLegado);
        u.Property(x => x.TermosAceitosEm).HasColumnType("timestamp without time zone").HasConversion(utcLegado);

        var p = m.Entity<Prestador>();
        p.ToTable("prestadores");
        p.HasKey(x => x.UsuarioId);
        p.Property(x => x.UsuarioId).ValueGeneratedNever();
        p.Property(x => x.NomeFantasia).HasMaxLength(100).IsRequired();
        p.Property(x => x.PlanoAssinatura).HasColumnType("character(1)").HasDefaultValue("G");
        p.Property(x => x.StatusDisponibilidade).HasMaxLength(20).HasDefaultValue("DISPONIVEL");
        p.HasOne(x => x.Usuario).WithOne().HasForeignKey<Prestador>(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        var c = m.Entity<Categoria>();
        c.ToTable("categorias", t => t.HasCheckConstraint("ck_categorias_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'"));
        c.HasKey(x => x.Id);
        c.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        c.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        c.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("uq_categorias_slug");
        c.Property(x => x.Descricao).HasMaxLength(5000);
        c.Property(x => x.IconeUrl).HasMaxLength(255);
        c.Property(x => x.Ativo).HasDefaultValue(true);
        c.Property(x => x.CriadoEm).HasDefaultValueSql("CURRENT_TIMESTAMP");
        c.Property(x => x.AtualizadoEm).HasDefaultValueSql("CURRENT_TIMESTAMP");

        var v = m.Entity<PrestadorCategoria>();
        v.ToTable("prestador_categorias");
        v.HasKey(x => new { x.PrestadorId, x.CategoriaId });
        v.Property(x => x.AdicionadoEm).HasDefaultValueSql("CURRENT_TIMESTAMP");
        v.HasOne(x => x.Categoria).WithMany(x => x.PrestadoresCategorias)
            .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        v.HasOne(x => x.Prestador).WithMany(x => x.Categorias)
            .HasForeignKey(x => x.PrestadorId).OnDelete(DeleteBehavior.Restrict);

        // Convenção local explícita: todos os campos persistidos usam snake_case.
        foreach (var entidade in m.Model.GetEntityTypes())
            foreach (var propriedade in entidade.GetProperties())
                propriedade.SetColumnName(System.Text.RegularExpressions.Regex
                    .Replace(propriedade.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
    }
}
