using Conecta.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Conecta.API;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuracao = new ConfigurationBuilder().AddUserSecrets<AppDbContextFactory>(optional: true)
            .AddEnvironmentVariables().Build();
        var conexao = configuracao.GetConnectionString("Conecta")
            ?? throw new InvalidOperationException("Configure ConnectionStrings__Conecta ou user-secrets antes de executar migrations.");
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(conexao).Options);
    }
}
