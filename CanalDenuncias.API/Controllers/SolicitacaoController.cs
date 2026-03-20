using CanalDenuncias.Application.DTOs.Request;
using CanalDenuncias.Application.DTOs.Response;
using CanalDenuncias.Application.Interfaces;
using CanalDenuncias.Application.Results;
using CanalDenuncias.Application.Utlis;
using CanalDenuncias.Domain.Filters;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanalDenuncias.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SolicitacaoController : ControllerBase
{
    private readonly ISolicitacaoService _solicitacaoService;
    private readonly IValidator<SolicitacaoRequest> _validator;

    public SolicitacaoController(
        ISolicitacaoService solicitacaoService,
        IValidator<SolicitacaoRequest> validator)
    {
        _solicitacaoService = solicitacaoService;
        _validator = validator;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(30_000_000)]
    public async Task<ActionResult<string>> CriarSolicitacao(
        [FromForm] SolicitacaoRequest request,
        CancellationToken cancellationToken)
    {
        // Validação dos dados de entrada usando FluentValidation
        // Por causa do usuário, quando não anônimo
        // É usada a validação do FluentValidation no DTO SolicitacaoRequest
        var validation = await _validator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        }

        var resultado = await _solicitacaoService.CriarSolicitacaoAsync(
            request,
            request.Anexos,
            cancellationToken);

        if (resultado.IsFailure)
        {
            if (resultado.Errors.Any(e => e.Code == ErrorsEnum.DOMAIN_VALIDATION.ToString()))
                return BadRequest(resultado.Errors);

            return StatusCode(StatusCodes.Status500InternalServerError, resultado.Errors);
        }

        return CreatedAtAction(nameof(CriarSolicitacao),
            new { protocolo = resultado.Value }, resultado.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SolicitacaoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<ActionResult<IEnumerable<SolicitacaoResponse>>> BuscarSolicitacoes(
        [FromQuery] SolicitacaoFilter filtro,
        CancellationToken cancellationToken)
    {
        // Chame o serviço (que por sua vez chamará o repositório)
        var resultado = await _solicitacaoService.ListarSolicitacoesAsync(filtro, cancellationToken);

        if (resultado.IsFailure)
        {
            if (resultado.Errors.Any(e => e.Code == ErrorsEnum.VALIDATION.ToString()))
                return BadRequest(resultado.Errors);
            else if (resultado.Errors.Any(e => e.Code == ErrorsEnum.NOT_FOUND.ToString()))
                return NotFound(resultado.Errors);

            return StatusCode(StatusCodes.Status500InternalServerError, resultado.Errors);
        }

        return Ok(resultado.Value);
    }

    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<ActionResult<bool>> AtualizarStatus(
        [FromQuery] string protocolo,
        [FromQuery] int novoStatusId,
        CancellationToken cancellationToken)
    {
        var resultado = await _solicitacaoService.AtualizarStatus(protocolo, novoStatusId, cancellationToken);
        if (resultado.IsFailure)
        {
            if (resultado.Errors.Any(e => e.Code == ErrorsEnum.VALIDATION.ToString()))
                return BadRequest(resultado.Errors);
            else if (resultado.Errors.Any(e => e.Code == ErrorsEnum.NOT_FOUND.ToString()))
                return NotFound(resultado.Errors);

            return StatusCode(StatusCodes.Status500InternalServerError, resultado.Errors);
        }

        return NoContent();
    }

    [HttpGet("{protocolo}/anexos/{anexoId:int}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IEnumerable<ErrorDto>), StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<IActionResult> DownloadAnexo(
    [FromRoute] string protocolo,
    [FromRoute] int anexoId,
    CancellationToken cancellationToken)
    {
        var resultado = await _solicitacaoService.DownloadAnexoAsync(protocolo, anexoId, cancellationToken);

        if (resultado.IsFailure)
        {
            if (resultado.Errors.Any(e => e.Code == ErrorsEnum.VALIDATION.ToString()))
                return BadRequest(resultado.Errors);
            if (resultado.Errors.Any(e => e.Code == ErrorsEnum.NOT_FOUND.ToString()))
                return NotFound(resultado.Errors);

            return StatusCode(StatusCodes.Status500InternalServerError, resultado.Errors);
        }

        var (stream, contentType, downloadName) = resultado.Value;
        return File(stream, contentType, downloadName);
    }
}