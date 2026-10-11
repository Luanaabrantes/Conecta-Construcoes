using System.Data;

namespace Conecta.Domain.Interfaces;

/// <summary>
/// Contrato para fábrica de conexões com o banco de dados (Dapper/ADO.NET).
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Cria e retorna uma nova instância de conexão com o banco de dados.
    /// </summary>
    IDbConnection CreateConnection();
}
