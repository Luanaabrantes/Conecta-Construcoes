using Conecta.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Conecta.Infrastructure.Database;

/// <summary>Evolve é a única fonte de versionamento do schema. V1/V2 da equipe são preservadas.</summary>
public class EvolveDatabaseMigrator(IConfiguration configuration, ILogger<EvolveDatabaseMigrator> logger)
{
    public void Migrate()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Database", "Migrations");
        if (!Directory.Exists(caminho)) throw new InvalidOperationException("Scripts Evolve ausentes na publicação.");
        using var conexao = new NpgsqlConnection(ConfiguracaoBanco.ObterConexao(configuration));
        conexao.Open();
        using (var legado = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema=current_schema() AND table_name='__EFMigrationsHistory')", conexao))
            if ((bool)legado.ExecuteScalar()!)
                throw new InvalidOperationException("Banco legado de migrations EF detectado. Não misture os históricos. Faça a conciliação/baseline descrita em CON_012.md antes de executar Evolve; nenhum dado foi alterado.");
        using var consultarSchema = new NpgsqlCommand("SELECT current_schema()", conexao);
        var schema = (string?)consultarSchema.ExecuteScalar()
            ?? throw new InvalidOperationException("SearchPath deve selecionar um schema existente.");
        var evolve = new EvolveDb.Evolve(conexao, mensagem => logger.LogInformation("[Evolve] {Mensagem}", mensagem))
        {
            Locations = [caminho], Schemas = [schema], MetadataTableSchema = schema,
            IsEraseDisabled = true, CommandTimeout = 120
        };
        evolve.Migrate();
    }
}
