using System.Data;
using Conecta.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Conecta.Infrastructure.Data;

/// <summary>
/// Fábrica de conexões PostgreSQL para consultas otimizadas via Dapper.
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = ConfiguracaoBanco.ObterConexao(configuration);
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
