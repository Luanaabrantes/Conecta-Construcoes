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
public sealed class CategoriasController(CategoriaServico categoriaServico) : ControllerBase
{
    /// <summary>Lista apenas categorias ativas. Acesso público.</summary>
    [HttpGet, AllowAnonymous]
    [ProducesResponseType<List<CategoriaDto>>(200)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) => Ok(await categoriaServico.Listar(false, cancellationToken));

    /// <summary>Consulta categoria ativa pelo slug normalizado. Acesso público.</summary>
    [HttpGet("{slug}"), AllowAnonymous]
    [ProducesResponseType<CategoriaDto>(200)]
    public async Task<IActionResult> PorSlug(string slug, CancellationToken cancellationToken) => Ok(await categoriaServico.PorSlug(slug, cancellationToken));

    /// <summary>Lista categorias ativas e inativas. Requer ADMIN.</summary>
    [HttpGet("/api/admin/categorias"), Authorize(Roles = "ADMIN")]
    [ProducesResponseType<List<CategoriaDto>>(200)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> ListarAdmin(CancellationToken cancellationToken) => Ok(await categoriaServico.Listar(true, cancellationToken));

    /// <summary>Cadastra uma categoria ativa. Requer ADMIN.</summary>
    [HttpPost, Authorize(Roles = "ADMIN")]
    [ProducesResponseType<CategoriaDto>(201)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Criar(CategoriaEntrada entrada, CancellationToken cancellationToken)
    {
        var categoriaCriada = await categoriaServico.Criar(entrada, cancellationToken);
        return CreatedAtAction(nameof(PorSlug), new { slug = categoriaCriada.Slug }, categoriaCriada);
    }

    /// <summary>Edita os dados da categoria. Requer ADMIN.</summary>
    [HttpPut("{id:int}"), Authorize(Roles = "ADMIN")]
    [ProducesResponseType<CategoriaDto>(200)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Editar([FromRoute(Name = "id")] int categoriaId, CategoriaEntrada entrada, CancellationToken cancellationToken) => Ok(await categoriaServico.Editar(categoriaId, entrada, cancellationToken));

    /// <summary>Ativa ou desativa uma categoria sem remover suas especialidades. Requer ADMIN.</summary>
    [HttpPatch("{id:int}/ativo"), Authorize(Roles = "ADMIN")]
    [ProducesResponseType<CategoriaDto>(200)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Ativo([FromRoute(Name = "id")] int categoriaId, AlterarAtivoEntrada entrada, CancellationToken cancellationToken) => Ok(await categoriaServico.AlterarAtivo(categoriaId, entrada.Ativo!.Value, cancellationToken));
}