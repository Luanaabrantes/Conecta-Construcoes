using Conecta.Application.Categorias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conecta.API.Controllers;

[ApiController]
[Route("api/prestadores/me/categorias")]
[Authorize(Roles = "PRESTADOR")]
[Produces("application/json")]
[ProducesResponseType(401), ProducesResponseType(403)]
[ProducesResponseType<ProblemDetails>(404)]
public sealed class EspecialidadesController(CategoriaServico servico) : ControllerBase
{
    private int PrestadorId => int.Parse(User.FindFirst("sub")!.Value, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Lista as próprias especialidades, inclusive as desativadas. Requer PRESTADOR.</summary>
    [HttpGet]
    [ProducesResponseType<List<EspecialidadeDto>>(200)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await servico.Especialidades(PrestadorId, ct));

    /// <summary>Associa categoria ativa ao próprio perfil. Repetições retornam 204. Requer PRESTADOR.</summary>
    [HttpPost("{categoriaId:int}")]
    [ProducesResponseType(204), ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Adicionar(int categoriaId, CancellationToken ct)
    {
        await servico.AdicionarEspecialidade(PrestadorId, categoriaId, ct);
        return NoContent();
    }

    /// <summary>Remove a própria especialidade mesmo inativa. Repetições retornam 204. Requer PRESTADOR.</summary>
    [HttpDelete("{categoriaId:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Remover(int categoriaId, CancellationToken ct)
    {
        await servico.RemoverEspecialidade(PrestadorId, categoriaId, ct);
        return NoContent();
    }
}