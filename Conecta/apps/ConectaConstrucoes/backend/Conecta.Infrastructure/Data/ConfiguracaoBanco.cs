using Microsoft.Extensions.Configuration;

namespace Conecta.Infrastructure.Data;

public static class ConfiguracaoBanco
{
    public static string ObterConexao(IConfiguration configuracao)
    {
        var conexao = configuracao.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conexao)) conexao = configuracao.GetConnectionString("Conecta");
        return !string.IsNullOrWhiteSpace(conexao) ? conexao
            : throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection (ou o alias legado ConnectionStrings__Conecta) por variável de ambiente/user-secrets.");
    }
}
