using System.Globalization;
using System.Text;
using Conecta.Application.Categorias;
using Conecta.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
string Obrigatorio(string nome) => builder.Configuration[nome] is { Length: > 0 } valor
    ? valor : throw new InvalidOperationException($"Configure {nome} por variável de ambiente ou user-secrets.");
var conexao = Obrigatorio("ConnectionStrings:Conecta");
var emissor = Obrigatorio("Jwt:Issuer");
var audiencia = Obrigatorio("Jwt:Audience");
var chave = Obrigatorio("Jwt:SigningKey");
if (Encoding.UTF8.GetByteCount(chave) < 32) throw new InvalidOperationException("Jwt:SigningKey deve ter pelo menos 32 bytes.");

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conexao));
builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<CategoriaServico>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = emissor,
        ValidateAudience = true, ValidAudience = audiencia,
        ValidateLifetime = true, RequireExpirationTime = true,
        RequireSignedTokens = true, ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        NameClaimType = "sub", RoleClaimType = "role", ClockSkew = TimeSpan.FromSeconds(30)
    };
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = contexto =>
        {
            var sujeitos = contexto.Principal!.FindAll("sub").ToList();
            if (sujeitos.Count != 1 || !int.TryParse(sujeitos[0].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out var id) || id <= 0)
                contexto.Fail("Identidade inválida.");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "ConectaConstruções — Categorias e Especialidades", Version = "v1" });
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Conecta.API.xml"));
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", Description = "Token validado: sub numérico e role ADMIN/PRESTADOR." });
    o.OperationFilter<SegurancaSwagger>();
});

var app = builder.Build();
app.UseExceptionHandler(tratamento => tratamento.Run(async contexto =>
{
    var erro = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;
    var status = erro is ErroModulo modulo ? modulo.Status : 500;
    await Results.Problem(statusCode: status, title: erro is ErroModulo conhecido
        ? conhecido.Message : "Erro interno ao processar a solicitação.").ExecuteAsync(contexto);
}));
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;

public sealed class SegurancaSwagger : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var atributos = context.MethodInfo.DeclaringType!.GetCustomAttributes(true)
            .Concat(context.MethodInfo.GetCustomAttributes(true)).ToList();
        if (atributos.OfType<AllowAnonymousAttribute>().Any() || !atributos.OfType<AuthorizeAttribute>().Any()) return;
        operation.Security = [new OpenApiSecurityRequirement
        { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] }];
    }
}