using CanalDenuncias.Application.DTOs.Request;
using CanalDenuncias.Application.DTOs.Response;
using CanalDenuncias.Application.Interfaces;
using CanalDenuncias.Application.Results;
using CanalDenuncias.Application.Utlis;
using CanalDenuncias.Domain.Entities;
using CanalDenuncias.Domain.Exceptions;
using CanalDenuncias.Domain.Filters;
using CanalDenuncias.Domain.Repositories;
using CanalDenuncias.Infra.EmailService.Services;
using CanalDenuncias.Infra.FileStorage.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CanalDenuncias.Application.Services;

public class SolicitacaoService : ISolicitacaoService
{
    private readonly ISolicitacaoRepository _solicitacaoRepository;
    private readonly IMensagemRepository _mensagemRepository;
    private readonly IUnitOfWork _uow;
    private readonly ISendEmail _sendEmail;
    private readonly ISolicitacaoAnexoRepository _solicitacaoAnexoRepository;

    private readonly IAnexoStorageService _anexoStorage;
    private readonly ILogger<SolicitacaoService> _logger;

    private const int VINCULOOUTROS = 5;

    public SolicitacaoService(
        ISolicitacaoRepository solicitacaoRepository,
        IMensagemRepository mensagemRepository,
        IUnitOfWork uow,
        ISendEmail sendEmail,
        IAnexoStorageService anexoStorageService,
        ISolicitacaoAnexoRepository solicitacaoAnexoRepository,
        ILogger<SolicitacaoService> logger)
    {
        _solicitacaoRepository = solicitacaoRepository;
        _mensagemRepository = mensagemRepository;
        _uow = uow;
        _sendEmail = sendEmail;
        _solicitacaoAnexoRepository = solicitacaoAnexoRepository;

        _anexoStorage = anexoStorageService;
        _logger = logger;
    }

    public async Task<Result<string>> CriarSolicitacaoAsync(
        SolicitacaoRequest request,
        List<IFormFile>? anexos,
        CancellationToken ct)
    {
        string? protocolo = null;
        bool uploaded = false;
        const long maxSizePerFile = 10 * 1024 * 1024; // 10MB

        if (anexos?.Count > 5)
            return Result<string>.Failure(
                new ErrorDto(ErrorsEnum.VALIDATION.ToString(), "Máximo de 5 anexos permitido.")
            );

        if (anexos?.Any(a => a.Length > maxSizePerFile) == true)
        {
            return Result<string>.Failure(
                new ErrorDto("VALIDATION", "Cada anexo deve ter no máximo 10MB.")
            );
        }

        try
        {
            Usuario? usuario = null;

            if (!request.Anonimo)
                usuario = new Usuario(request.Nome, request.Telefone, request.Email, request.Cpf);

            var solicitacao = new Solicitacao(
                anonima: request.Anonimo,
                identificadorColaboradorOuSetor: request.IdentificadorSetorOuPessoa!,
                testemunhas: request.Testemunhas,
                evidencias: request.Evidencias,
                dataOcorrido: request.DataOcorrido,
                usuario: usuario,
                vinculoId: request.Vinculo ?? VINCULOOUTROS // Outros
            );

            protocolo = solicitacao.Protocolo;

            // 1) UPLOAD PRIMEIRO (se houver anexos)
            if (anexos?.Count > 0)
            {
                var anexosCriados = await UploadEMapearAnexosAsync(solicitacao, anexos, ct);
                foreach (var a in anexosCriados)
                    _solicitacaoAnexoRepository.Add(a);

                uploaded = true;
            }

            // 2) Persistência no DB
            var mensagem = new Mensagem(request.Mensagem, solicitacao, null);

            _solicitacaoRepository.Add(solicitacao);
            _mensagemRepository.Add(mensagem);

            await _uow.CommitAsync(CancellationToken.None);

            await _sendEmail.SendEmailSolicitacaoAsync(protocolo, usuario?.Email!);

            return Result<string>.Success(protocolo);
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(new ErrorDto(
                Code: ErrorsEnum.DOMAIN_VALIDATION.ToString(),
                Message: ex.Message
            ));
        }
        catch (IOException)
        {
            return Result<string>.Failure(
                new ErrorDto(
                    Code: ErrorsEnum.UPLOAD_ERROR.ToString(),
                    Message: "Erro ao enviar anexos."
                )
            );
        }
        catch (Exception)
        {
            // 3) Compensação: se subiu algo e falhou o commit, apaga do storage
            if (uploaded && !string.IsNullOrWhiteSpace(protocolo))
            {
                try
                {
                    await _anexoStorage.DeleteByProtocoloAsync(protocolo, ct);
                }
                catch
                {
                    _logger.LogError(@"Falha ao tentar compensar upload de anexos
                        para a solicitação {Protocolo} após erro no registro da solicitação.
                        Favor verificar manualmente.", protocolo);
                }
            }

            return Result<string>.Failure(new ErrorDto(
                Code: ErrorsEnum.INTERNAL_ERROR.ToString(),
                Message: "Ocorreu um erro inesperado ao registrar a solicitação."
            ));
        }
    }

    private async Task<List<SolicitacaoAnexo>> UploadEMapearAnexosAsync(
        Solicitacao solicitacao,
        List<IFormFile> anexos,
        CancellationToken ct)
    {
        const int maxParalelo = 4;
        using var sem = new SemaphoreSlim(maxParalelo);

        var tasks = anexos.Select(async file =>
        {
            await sem.WaitAsync(ct);
            try
            {
                var savedName = await _anexoStorage.UploadAsync(solicitacao.Protocolo, file, ct);

                return new SolicitacaoAnexo(
                    solicitacao: solicitacao,
                    protocolo: solicitacao.Protocolo,
                    nomeArquivo: savedName,
                    nomeOriginal: Path.GetFileName(file.FileName),
                    contentType: file.ContentType,
                    tamanhoOriginal: file.Length,
                    compactado: true 
                );
            }
            finally
            {
                sem.Release();
            }
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    public async Task<Result<IEnumerable<SolicitacaoResponse>>> ListarSolicitacoesAsync(
        SolicitacaoFilter filtro, CancellationToken cancellationToken)
    {
        if (filtro.DataFinal < filtro.DataInicial)
            return Result<IEnumerable<SolicitacaoResponse>>.Failure(new ErrorDto(
                Code: ErrorsEnum.VALIDATION.ToString(),
                Message: "A data final deve ser maior ou igual à data inicial."
            ));

        try
        {
            var solicitacoes = await _solicitacaoRepository.ListarSolicitacoesAsync(filtro, cancellationToken);
            if (solicitacoes == null)
                return Result<IEnumerable<SolicitacaoResponse>>.Failure(new ErrorDto(
                    Code: ErrorsEnum.NOT_FOUND.ToString(),
                    Message: "Nenhuma solicitação encontrada com o status e período informados."
                ));

            return Result<IEnumerable<SolicitacaoResponse>>
                .Success(solicitacoes.Select(s => new SolicitacaoResponse
                {
                    Id = s.Id,
                    Protocolo = s.Protocolo,
                    DataCriacao = s.DataCriacao,
                    DataOCorrencia = s.DataOcorrencia,
                    Status = s.StatusSolicitacao!.Descricao,
                    Anonima = s.Anonima,
                    Nome = s.Usuario?.Nome,
                    Email = s.Usuario?.Email,
                    Telefone = s.Usuario?.Telefone,
                    Cpf = s.Usuario?.CPF,
                    Anexos = s.Anexos?.Select(a => new SolicitacaoAnexoResponse
                    {
                        Id = a.Id,
                        NomeOriginal = a.NomeOriginal,
                        NomeArquivo = a.NomeArquivo,
                        ContentType = a.ContentType,
                        TamanhoOriginal = a.TamanhoOriginal
                    })
                }));
        }
        catch (Exception)
        {
            return Result<IEnumerable<SolicitacaoResponse>>.Failure(new ErrorDto(
                Code: ErrorsEnum.INTERNAL_ERROR.ToString(),
                Message: "Ocorreu um erro inesperado ao buscar as solicitações."
            ));
        }
    }

    public async Task<Result<bool>> AtualizarStatus(
        string protocolo, int novoStatusId, CancellationToken cancellationToken)
    {
        try
        {
            var solicitacao =
                await _solicitacaoRepository.ObterSolicitacaoPorProtocoloAsync(protocolo, cancellationToken);

            if (solicitacao == null)
            {
                return Result<bool>.Failure(new ErrorDto(
                    Code: ErrorsEnum.NOT_FOUND.ToString(),
                    Message: "Solicitação não encontrada."
                ));
            }

            await _solicitacaoRepository.AtualizarStatus(solicitacao, novoStatusId, cancellationToken);
            await _uow.CommitAsync(cancellationToken);           

            if (solicitacao.Usuario?.Email != null)
            {
                // Necessário buscar novamente para garantir que o novo Status seja carregado.
                solicitacao = await _solicitacaoRepository
                    .ObterSolicitacaoPorProtocoloAsync(protocolo, cancellationToken);

                await _sendEmail.SendEmailStatusChangeAsync(solicitacao!);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception)
        {
            return Result<bool>.Failure(new ErrorDto(
                Code: ErrorsEnum.INTERNAL_ERROR.ToString(),
                Message: "Ocorreu um erro inesperado ao alterar o status das solicitações."
            ));
        }
    }

    public async Task<Result<(Stream Stream, string ContentType, string DownloadName)>> DownloadAnexoAsync(
        string protocolo, int anexoId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(protocolo) || anexoId <= 0)
            return Result<(Stream, string, string)>.Failure(new ErrorDto(
                Code: ErrorsEnum.VALIDATION.ToString(),
                Message: "Parâmetros inválidos."
            ));

        var anexo = await _solicitacaoAnexoRepository.ObterAnexoAsync(anexoId, ct);

        // Segurança: garante que o anexo pertence ao protocolo informado
        if (anexo is null || anexo.Protocolo != protocolo)
            return Result<(Stream, string, string)>.Failure(new ErrorDto(
                Code: ErrorsEnum.NOT_FOUND.ToString(),
                Message: "Anexo não encontrado."
            ));

        try
        {
            var stream = await _anexoStorage.OpenReadAsync(
                protocolo,
                anexo.NomeArquivo,
                decompress: anexo.Compactado,
                ct);

            var contentType = string.IsNullOrWhiteSpace(anexo.ContentType)
                ? "application/octet-stream"
                : anexo.ContentType;

            var downloadName = string.IsNullOrWhiteSpace(anexo.NomeOriginal)
                ? anexo.NomeArquivo
                : anexo.NomeOriginal;

            return Result<(Stream, string, string)>.Success((stream, contentType, downloadName));
        }
        catch (FileNotFoundException)
        {
            return Result<(Stream, string, string)>.Failure(new ErrorDto(
                Code: ErrorsEnum.NOT_FOUND.ToString(),
                Message: "Arquivo não encontrado no storage."
            ));
        }
        catch (IOException)
        {
            return Result<(Stream, string, string)>.Failure(new ErrorDto(
                Code: ErrorsEnum.INTERNAL_ERROR.ToString(),
                Message: "Erro ao ler o arquivo."
            ));
        }
    }
}