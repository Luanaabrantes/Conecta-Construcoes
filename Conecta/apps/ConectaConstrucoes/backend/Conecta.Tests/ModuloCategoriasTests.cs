using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Conecta.Application.Categorias;
using Conecta.Domain.Entidades;
using Conecta.Infrastructure;
using Conecta.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Conecta.Infrastructure.Data;
using Conecta.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;

namespace Conecta.Tests;

public sealed class Ambiente : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string Chave { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly string esquema = "con012_test_" + Guid.NewGuid().ToString("N");
    public string Conexao { get; private set; } = "";
    private string conexaoBase = "";
    public AppDbContext Banco() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(Conexao).Options);
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", Conexao);
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Jwt:Issuer", "con012-test");
        builder.UseSetting("Jwt:Audience", "con012-test-api");
        builder.UseSetting("Jwt:SigningKey", Chave);
    }
    public async Task InitializeAsync()
    {
        conexaoBase = Environment.GetEnvironmentVariable("CONECTA_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Configure CONECTA_TEST_CONNECTION para um PostgreSQL de teste. Nenhum teste de integração será simulado.");
        await using var cn = new NpgsqlConnection(conexaoBase);
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand($"CREATE SCHEMA {esquema}", cn);
        await cmd.ExecuteNonQueryAsync();
        Conexao = new NpgsqlConnectionStringBuilder(conexaoBase) { SearchPath = esquema }.ConnectionString;
        // Prepara V1/V2 e dados reais de domínio antes de V3 para verificar atualização sem perda.
        using (var preparacao = new NpgsqlConnection(Conexao))
            new EvolveDb.Evolve(preparacao)
            {
                Locations = [Path.Combine(AppContext.BaseDirectory, "Database", "Migrations")],
                Schemas = [esquema], MetadataTableSchema = esquema, IsEraseDisabled = true,
                TargetVersion = new EvolveDb.Migration.MigrationVersion("2.0")
            }.Migrate();
        await using (var preexistentes = new NpgsqlConnection(Conexao))
        {
            await preexistentes.OpenAsync();
            await using var inserir = new NpgsqlCommand("INSERT INTO usuarios(id,email,senha_hash,perfil) VALUES (1001,'con012-upgrade@example.invalid','SEM-LOGIN-TESTE','PRESTADOR'); INSERT INTO prestadores(usuario_id,nome_fantasia) VALUES (1001,'CON012 Upgrade'); INSERT INTO categorias(id,nome,slug,descricao,ativo) VALUES (1001,'CON012 Preexistente','con012-preexistente','Preservar',false); INSERT INTO prestador_categorias(prestador_id,categoria_id) VALUES (1001,1001)", preexistentes);
            await inserir.ExecuteNonQueryAsync();
        }
        await using var db = Banco();
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:DefaultConnection"] = Conexao }).Build();
        new EvolveDatabaseMigrator(configuracao, NullLogger<EvolveDatabaseMigrator>.Instance).Migrate();
        db.Usuarios.AddRange(Enumerable.Range(101, 2).Select(id => new Usuario
        { Id = id, Email = $"con012-test-{id}@example.invalid", SenhaHash = "SEM-LOGIN-TESTE", Perfil = "PRESTADOR" }));
        db.Prestadores.AddRange(Enumerable.Range(101, 2).Select(id => new Prestador { UsuarioId = id, NomeFantasia = $"CON012 Teste {id}" }));
        await db.SaveChangesAsync();
    }
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (string.IsNullOrEmpty(conexaoBase)) return;
        // Só remove o schema aleatório criado por esta fixture, nunca public/banco/volume.
        await using var cn = new NpgsqlConnection(conexaoBase);
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP SCHEMA {esquema} CASCADE", cn);
        await cmd.ExecuteNonQueryAsync();
    }
    public HttpClient Cliente(string? perfil = null, int id = 101, string? chave = null,
        string emissor = "con012-test", string audiencia = "con012-test-api", bool expirado = false)
    {
        var cliente = CreateClient();
        if (perfil != null)
        {
            var agora = DateTime.UtcNow;
            var token = new JwtSecurityToken(emissor, audiencia,
                [new Claim("sub", id.ToString()), new Claim("role", perfil)],
                agora.AddMinutes(-10), expirado ? agora.AddMinutes(-5) : agora.AddMinutes(10),
                new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave ?? Chave)), SecurityAlgorithms.HmacSha256));
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        return cliente;
    }
}

public sealed class ModuloCategoriasTests(Ambiente ambiente) : IClassFixture<Ambiente>
{
    private static CategoriaEntrada Entrada(string? slug = null) => new("CON012 Teste", slug ?? "con012-" + Guid.NewGuid().ToString("N"), "Descrição", "https://example.invalid/icone.svg");
    private async Task<CategoriaDto> Criar()
    {
        using var cliente = ambiente.Cliente("ADMIN");
        var resposta = await cliente.PostAsJsonAsync("/api/categorias", Entrada());
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
        return (await resposta.Content.ReadFromJsonAsync<CategoriaDto>())!;
    }

    [Fact]
    public async Task PublicoFiltraInativasEConsultaSlug()
    {
        var c = await Criar();
        using var publico = ambiente.Cliente();
        var consultada = (await publico.GetFromJsonAsync<CategoriaDto>($"/api/categorias/{c.Slug}"))!; Assert.Equal(c.Id, consultada.Id); Assert.Equal(c.Slug, consultada.Slug);
        using var admin = ambiente.Cliente("ADMIN");
        Assert.Equal(HttpStatusCode.OK, (await admin.PatchAsJsonAsync($"/api/categorias/{c.Id}/ativo", new { ativo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync($"/api/categorias/{c.Slug}")).StatusCode);
        Assert.DoesNotContain((await publico.GetFromJsonAsync<List<CategoriaDto>>("/api/categorias"))!, x => x.Id == c.Id);
        Assert.Contains((await admin.GetFromJsonAsync<List<CategoriaDto>>("/api/admin/categorias"))!, x => x.Id == c.Id && !x.Ativo);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/categorias/inexistente-con012")).StatusCode);
    }

    [Theory]
    [InlineData("", "valido", null, null)]
    [InlineData("Nome", "", null, null)]
    [InlineData("Nome", "slug/ilegal", null, null)]
    [InlineData("Nome", "-slug", null, null)]
    [InlineData("Nome", "valido", null, "javascript:alert(1)")]
    public async Task EntradaInvalidaRetorna400(string nome, string slug, string? descricao, string? icone)
    {
        using var admin = ambiente.Cliente("ADMIN");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/categorias", new CategoriaEntrada(nome, slug, descricao, icone))).StatusCode);
    }

    [Fact]
    public async Task LimitesDosCampos()
    {
        using var admin = ambiente.Cliente("ADMIN");
        var entrada = Entrada();
        foreach (var invalida in new[] { entrada with { Nome = new string('n', 101) }, entrada with { Slug = new string('s', 101) },
            entrada with { Descricao = new string('d', 5001) }, entrada with { IconeUrl = "https://example.invalid/" + new string('i', 255) } })
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/categorias", invalida)).StatusCode);
    }

    [Fact]
    public async Task NormalizaSlugERejeitaDuplicadoConcorrente()
    {
        using var admin = ambiente.Cliente("ADMIN");
        var sufixo = Guid.NewGuid().ToString("N");
        var respostas = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => admin.PostAsJsonAsync("/api/categorias", Entrada("  Construção " + sufixo + "  "))));
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(3, respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var dto = (await respostas.Single(r => r.StatusCode == HttpStatusCode.Created).Content.ReadFromJsonAsync<CategoriaDto>())!;
        Assert.Equal("construcao-" + sufixo, dto.Slug);
        Assert.All(respostas.Where(r => r.StatusCode == HttpStatusCode.Conflict), r => Assert.DoesNotContain("Npgsql", r.Content.ReadAsStringAsync().Result));
    }

    [Fact]
    public async Task EdicaoAtivacaoETimestamps()
    {
        var c = await Criar();
        var outra = await Criar();
        using var admin = ambiente.Cliente("ADMIN");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/categorias/{c.Id}", Entrada(outra.Slug))).StatusCode);
        var r = await admin.PutAsJsonAsync($"/api/categorias/{c.Id}", Entrada(c.Slug) with { Nome = "Editada" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var editada = (await r.Content.ReadFromJsonAsync<CategoriaDto>())!;
        Assert.True(Math.Abs((c.CriadoEm - editada.CriadoEm).Ticks) < 10);
        Assert.True(editada.AtualizadoEm >= c.AtualizadoEm);
        Assert.Equal(DateTimeKind.Utc, editada.AtualizadoEm.Kind);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync("/api/categorias/2147483647", Entrada())).StatusCode);
        await admin.PatchAsJsonAsync($"/api/categorias/{c.Id}/ativo", new { ativo = false });
        await admin.PatchAsJsonAsync($"/api/categorias/{c.Id}/ativo", new { ativo = true });
        using var publico = ambiente.Cliente();
        Assert.Equal(HttpStatusCode.OK, (await publico.GetAsync($"/api/categorias/{c.Slug}")).StatusCode);
    }

    [Fact]
    public async Task EspecialidadesIdempotentesEIsoladas()
    {
        var c = await Criar();
        using var a = ambiente.Cliente("PRESTADOR", 101);
        using var b = ambiente.Cliente("PRESTADOR", 102);
        var url = $"/api/prestadores/me/categorias/{c.Id}";
        var respostas = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => a.PostAsync(url, null)));
        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.NoContent, r.StatusCode));
        Assert.Single((await a.GetFromJsonAsync<List<EspecialidadeDto>>("/api/prestadores/me/categorias"))!, x => x.Categoria.Id == c.Id);
        Assert.DoesNotContain((await b.GetFromJsonAsync<List<EspecialidadeDto>>("/api/prestadores/me/categorias"))!, x => x.Categoria.Id == c.Id);
        await b.DeleteAsync(url);
        Assert.Contains((await a.GetFromJsonAsync<List<EspecialidadeDto>>("/api/prestadores/me/categorias"))!, x => x.Categoria.Id == c.Id);
        using var admin = ambiente.Cliente("ADMIN");
        await admin.PatchAsJsonAsync($"/api/categorias/{c.Id}/ativo", new { ativo = false });
        Assert.Equal(HttpStatusCode.Conflict, (await b.PostAsync(url, null)).StatusCode);
        Assert.Contains((await a.GetFromJsonAsync<List<EspecialidadeDto>>("/api/prestadores/me/categorias"))!, x => x.Categoria.Id == c.Id && !x.Categoria.Ativo);
        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync(url)).StatusCode);
    }

    [Fact]
    public async Task CategoriaEPrestadorInexistentes()
    {
        using var prestador = ambiente.Cliente("PRESTADOR");
        Assert.Equal(HttpStatusCode.NotFound, (await prestador.PostAsync("/api/prestadores/me/categorias/2147483647", null)).StatusCode);
        using var inexistente = ambiente.Cliente("PRESTADOR", 2147483647);
        Assert.Equal(HttpStatusCode.NotFound, (await inexistente.GetAsync("/api/prestadores/me/categorias")).StatusCode);
        var c = await Criar();
        Assert.Equal(HttpStatusCode.NotFound, (await inexistente.PostAsync($"/api/prestadores/me/categorias/{c.Id}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await inexistente.DeleteAsync($"/api/prestadores/me/categorias/{c.Id}")).StatusCode);
    }

    [Fact]
    public async Task SemToken401EPerfilInadequado403()
    {
        using var anonimo = ambiente.Cliente();
        using var cliente = ambiente.Cliente("CLIENTE");
        var rotas = new[] { ("GET", "/api/admin/categorias"), ("POST", "/api/categorias"),
            ("PUT", "/api/categorias/1"), ("PATCH", "/api/categorias/1/ativo"),
            ("GET", "/api/prestadores/me/categorias"), ("POST", "/api/prestadores/me/categorias/1"), ("DELETE", "/api/prestadores/me/categorias/1") };
        foreach (var (metodo, url) in rotas)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), url))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), url))).StatusCode);
        }
        using var prestador = ambiente.Cliente("PRESTADOR");
        Assert.Equal(HttpStatusCode.Forbidden, (await prestador.GetAsync("/api/admin/categorias")).StatusCode);
        using var admin = ambiente.Cliente("ADMIN");
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/prestadores/me/categorias")).StatusCode);
    }

    [Fact]
    public async Task JwtValidaAssinaturaEmissorAudienciaExpiracaoESujeito()
    {
        foreach (var cliente in new[] { ambiente.Cliente("ADMIN", chave: new string('x', 48)),
            ambiente.Cliente("ADMIN", emissor: "outro"), ambiente.Cliente("ADMIN", audiencia: "outra"),
            ambiente.Cliente("ADMIN", expirado: true), ambiente.Cliente("ADMIN", id: 0) })
        {
            using (cliente) Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/admin/categorias")).StatusCode);
        }
        using var falso = ambiente.Cliente();
        falso.DefaultRequestHeaders.Add("X-User-Id", "101");
        falso.DefaultRequestHeaders.Add("X-Role", "ADMIN");
        Assert.Equal(HttpStatusCode.Unauthorized, (await falso.GetAsync("/api/admin/categorias")).StatusCode);
    }

    [Fact]
    public async Task PostgresImpedeSlugDuplicadoChaveCompostaEFks()
    {
        var c = await Criar();
        await using var cn = new NpgsqlConnection(ambiente.Conexao);
        await cn.OpenAsync();
        async Task Erro(string sql, string codigo)
        {
            await using var cmd = new NpgsqlCommand(sql, cn);
            var erro = await Assert.ThrowsAsync<PostgresException>(async () => await cmd.ExecuteNonQueryAsync());
            Assert.Equal(codigo, erro.SqlState);
        }
        await Erro($"INSERT INTO categorias(nome,slug) VALUES ('CON012 Teste','{c.Slug}')", "23505");
        await Erro($"INSERT INTO prestador_categorias(prestador_id,categoria_id) VALUES (2147483647,{c.Id})", "23503");
        await Erro("INSERT INTO prestador_categorias(prestador_id,categoria_id) VALUES (101,2147483647)", "23503");
        await using (var cmd = new NpgsqlCommand($"INSERT INTO prestador_categorias(prestador_id,categoria_id) VALUES (101,{c.Id})", cn)) await cmd.ExecuteNonQueryAsync();
        await Erro($"INSERT INTO prestador_categorias(prestador_id,categoria_id) VALUES (101,{c.Id})", "23505");
        await Erro($"DELETE FROM categorias WHERE id={c.Id}", "23503");
        await Erro("DELETE FROM prestadores WHERE usuario_id=101", "23503");
        await Erro("INSERT INTO prestadores(usuario_id,nome_fantasia) VALUES (2147483647,'CON012 Teste')", "23503");
        await Erro("INSERT INTO categorias(nome,slug) VALUES ('CON012 Teste','Slug-Invalido')", "23514");
    }

    [Fact]
    public async Task SwaggerEOpenApiDocumentamRotasEBearer()
    {
        using var publico = ambiente.Cliente();
        Assert.Equal(HttpStatusCode.OK, (await publico.GetAsync("/swagger/index.html")).StatusCode);
        foreach (var url in new[] { "/swagger/v1/swagger.json", "/openapi/v1.json" })
        {
            var resposta = await publico.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var caminhos = json.RootElement.GetProperty("paths");
            Assert.True(caminhos.TryGetProperty("/api/categorias", out _));
            Assert.True(caminhos.TryGetProperty("/api/admin/categorias", out _));
            Assert.True(caminhos.TryGetProperty("/api/prestadores/me/categorias/{categoriaId}", out _));
            if (url.StartsWith("/swagger"))
            {
                Assert.Equal("bearer", json.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
                Assert.True(caminhos.GetProperty("/api/admin/categorias").GetProperty("get").TryGetProperty("security", out _));
                Assert.False(caminhos.GetProperty("/api/categorias").GetProperty("get").TryGetProperty("security", out _));
            }
        }
    }

    [Fact]
    public async Task EvolveAtualizaV2SemPerderDadosEReexecucaoNaoDuplicaHistorico()
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        { ["ConnectionStrings:DefaultConnection"] = ambiente.Conexao }).Build();
        new EvolveDatabaseMigrator(configuracao, NullLogger<EvolveDatabaseMigrator>.Instance).Migrate();
        await using var banco = ambiente.Banco();
        var categoria = await banco.Categorias.SingleAsync(c => c.Id == 1001);
        Assert.Equal("Preservar", categoria.Descricao);
        Assert.False(categoria.Ativo);
        var vinculo = await banco.PrestadorCategorias.SingleAsync(v => v.PrestadorId == 1001 && v.CategoriaId == 1001);
        Assert.Equal(DateTimeKind.Utc, vinculo.AdicionadoEm.Kind);
        Assert.Equal(DateTimeKind.Utc, categoria.CriadoEm.Kind);
        await using var cn = new NpgsqlConnection(ambiente.Conexao);
        await cn.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT count(*) FROM changelog WHERE type=0 AND success=true",cn);
        Assert.Equal(3L, (long)(await comando.ExecuteScalarAsync())!);
        await using var historico = new NpgsqlCommand("SELECT count(*) FROM information_schema.tables WHERE table_schema=current_schema() AND table_name='__EFMigrationsHistory'",cn);
        Assert.Equal(0L, (long)(await historico.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task EvolveRecusaHistoricoEfSemAlterarDados()
    {
        var esquema = "con012_guard_" + Guid.NewGuid().ToString("N");
        await using var controle = new NpgsqlConnection(ambiente.Conexao);
        await controle.OpenAsync();
        await using (var criar = new NpgsqlCommand($"CREATE SCHEMA {esquema}; CREATE TABLE {esquema}.\"__EFMigrationsHistory\"(id int); INSERT INTO {esquema}.\"__EFMigrationsHistory\" VALUES (123)",controle)) await criar.ExecuteNonQueryAsync();
        try
        {
            var conexao = new NpgsqlConnectionStringBuilder(ambiente.Conexao) { SearchPath = esquema }.ConnectionString;
            var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
            { ["ConnectionStrings:DefaultConnection"] = conexao }).Build();
            var erro = Assert.Throws<InvalidOperationException>(() => new EvolveDatabaseMigrator(configuracao,NullLogger<EvolveDatabaseMigrator>.Instance).Migrate());
            Assert.Contains("Banco legado", erro.Message);
            await using var conferir = new NpgsqlCommand($"SELECT id FROM {esquema}.\"__EFMigrationsHistory\"",controle);
            Assert.Equal(123, (int)(await conferir.ExecuteScalarAsync())!);
        }
        finally
        {
            await using var remover = new NpgsqlCommand($"DROP SCHEMA {esquema} CASCADE",controle);
            await remover.ExecuteNonQueryAsync();
        }
    }

    [Theory]
    [InlineData("DefaultConnection")]
    [InlineData("Conecta")]
    public async Task EfEDapperUsamAMesmaConexaoEUmUnicoContexto(string nome)
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        { [$"ConnectionStrings:{nome}"] = ambiente.Conexao }).Build();
        var servicos = new ServiceCollection();
        servicos.AddInfrastructure(configuracao);
        servicos.AddSingleton<IConfiguration>(configuracao);
        Assert.Single(servicos, s => s.ServiceType == typeof(AppDbContext));
        await using var provider = servicos.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var esperado = new NpgsqlConnectionStringBuilder(ambiente.Conexao); var ef = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()); Assert.Equal((esperado.Host, esperado.Port, esperado.Database, esperado.SearchPath), (ef.Host, ef.Port, ef.Database, ef.SearchPath));
        using var cn = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>().CreateConnection();
        cn.Open();
        Assert.Equal("monorepo_db", cn.Database);
        var dapper = new NpgsqlConnectionStringBuilder(cn.ConnectionString); Assert.Equal((esperado.Host, esperado.Port, esperado.Database, esperado.SearchPath), (dapper.Host, dapper.Port, dapper.Database, dapper.SearchPath));
    }

    [Fact]
    public async Task CorsPermiteFrontendDaEquipe()
    {
        using var cliente = ambiente.Cliente();
        using var requisicao = new HttpRequestMessage(HttpMethod.Options,"/api/categorias");
        requisicao.Headers.Add("Origin","http://localhost:5173");
        requisicao.Headers.Add("Access-Control-Request-Method","POST");
        var resposta = await cliente.SendAsync(requisicao);
        Assert.Equal(HttpStatusCode.NoContent,resposta.StatusCode);
        Assert.Equal("http://localhost:5173",resposta.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
