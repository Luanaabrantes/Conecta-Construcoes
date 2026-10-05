using Conecta.Application.Categorias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conecta.API.Controllers;

[ApiController]
[Route("api/categorias")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
public sealed class CategoriasController(CategoriaServico servico) : ControllerBase
{
    /// <summary>Lista apenas categorias ativas. Acesso público.</summary>
    [HttpGet, AllowAnonymous]
    [ProducesResponseType<List<CategoriaDto>>(200)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await servico.Listar(false, ct));

    /// <summary>Consulta categoria ativa pelo slug normalizado. Acesso público.</summary>
    [HttpGet("{slug}"), AllowAnonymous]
    [ProducesResponseType<CategoriaDto>(200)]
    public async Task<IActionResult> PorSlug(string slug, CancellationToken ct) => Ok(await servico.PorSlug(slug, ct));

    /// <summary>Lista categorias ativas e inativas. Requer ADMIN.</summary>
    [HttpGet("/api/admin/categorias"), Authorize(Roles = "ADMIN")]
    [ProducesResponseType<List<CategoriaDto>>(200)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> ListarAdmin(CancellationToken ct) => Ok(await servico.Listar(true, ct));

    /// <summary>Cadastra uma categoria ativa. Requer ADMIN.</summary>
    [HttpPost, Authorize(Roles = "ADMIN")]
    [ProducesResponseType<CategoriaDto>(201)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Criar(CategoriaEntrada entrada, CancellationToken ct)
    {
        var dto = await servico.Criar(entrada, ct);
        return CreatedAtAction(nameof(PorSlug), new { slug = dto.Slug }, dto);
    }

    /// <summary>Edita os dados da categoria. Requer ADMIN.</summary>
    [HttpPut("{id:int}"), Authorize(Roles = "ADMIN")]
    [ProducesResponseType<CategoriaDto>(200)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Editar(int id, CategoriaEntrada entrada, CancellationToken ct) => Ok(await servico.Editar(id, entrada, ct));

    /// <summary>Ativa ou desativa uma categoria sem remover suas especialidades. Requer ADMIN.</summary>
    [HttpPatch("{id:int}/ativo"), Authorize(Roles = "ADMIN")]
    [ProducesResponseType<CategoriaDto>(200)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Ativo(int id, AlterarAtivoEntrada entrada, CancellationToken ct) => Ok(await servico.AlterarAtivo(id, entrada.Ativo!.Value, ct));
}