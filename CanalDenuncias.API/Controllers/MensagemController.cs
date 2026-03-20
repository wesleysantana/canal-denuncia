using CanalDenuncias.Application.DTOs.Request;
using CanalDenuncias.Application.DTOs.Response;
using CanalDenuncias.Application.Interfaces;
using CanalDenuncias.Application.Results;
using CanalDenuncias.Application.Utlis;
using Microsoft.AspNetCore.Mvc;

namespace CanalDenuncias.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MensagemController : ControllerBase
{
    private readonly IMensagemService _mensagemService;

    public MensagemController(IMensagemService mensagemService)
    {
        _mensagemService = mensagemService;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> CriarMensagem(
        [FromBody] MensagemRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioLogado = User?.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        var resultado = await _mensagemService
            .CriarMensagemAsync(request, usuarioLogado, cancellationToken);

        if (resultado.IsFailure)
        {
            if (resultado.Errors.Any(e => e.Code == ErrorsEnum.DOMAIN_VALIDATION.ToString()))
                return BadRequest(resultado.Errors);
            if (resultado.Errors.Any(e => e.Code == ErrorsEnum.NOT_FOUND.ToString()))
                return NotFound(resultado.Errors);

            return StatusCode(StatusCodes.Status500InternalServerError, resultado.Errors);
        }

        return CreatedAtAction(nameof(CriarMensagem), resultado.Value, resultado.Value);
    }

    [HttpGet("protocolo/{protocolo}")]
    [ProducesResponseType(typeof(IEnumerable<MensagemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MensagemResponse>>> ObterMensagensPorProtocolo(
        [FromRoute] string protocolo,
        CancellationToken cancellationToken)
    {
        var resultado = await _mensagemService.ObterMensagensPorProtocoloAsync(protocolo, cancellationToken);

        if (resultado.IsFailure)
        {
            if (resultado.Errors.Any(e => e.Code == ErrorsEnum.NOT_FOUND.ToString()))
                return NotFound(resultado.Errors);

            return StatusCode(StatusCodes.Status500InternalServerError, resultado.Errors);
        }

        return Ok(resultado.Value);
    }
}