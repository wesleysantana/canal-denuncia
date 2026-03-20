using CanalDenuncias.Application.DTOs.Request;
using CanalDenuncias.Application.DTOs.Response;
using CanalDenuncias.Application.Interfaces;
using CanalDenuncias.Application.Results;
using CanalDenuncias.Application.Utlis;
using CanalDenuncias.Domain.Entities;
using CanalDenuncias.Domain.Exceptions;
using CanalDenuncias.Domain.Repositories;
using CanalDenuncias.Infra.EmailService.Services;

namespace CanalDenuncias.Application.Services;

public class MensagemService : IMensagemService
{
    private readonly ISolicitacaoRepository _solicitacaoRepository;
    private readonly IMensagemRepository _mensagemRepository;
    private readonly IUnitOfWork _uow;
    private readonly ISendEmail _sendEmail;

    public MensagemService(
        ISolicitacaoRepository solicitacaoRepository,
        IMensagemRepository mensagemRepository,
        IUnitOfWork uow,
        ISendEmail sendEmail)
    {
        _solicitacaoRepository = solicitacaoRepository;
        _mensagemRepository = mensagemRepository;
        _uow = uow;
        _sendEmail = sendEmail;
    }

    public async Task<Result<string>> CriarMensagemAsync(
        MensagemRequest msgRequest, string? userLogado, CancellationToken cancellationToken)
    {
        try
        {
            var solicitacao = await _solicitacaoRepository
                .ObterSolicitacaoPorProtocoloAsync(msgRequest.Protocolo, cancellationToken);

            if (solicitacao is null)
                return Result<string>.Failure(new ErrorDto(
                  Code: ErrorsEnum.NOT_FOUND.ToString(),
                  Message: "Solicitação não encontrada."
                ));

            var mensagem = new Mensagem(msgRequest.Conteudo, solicitacao, userLogado);
            _mensagemRepository.Add(mensagem);
            await _uow.CommitAsync(cancellationToken);

            // Significa que a mensagem foi criada por um usuário logado, ou seja,
            // é uma resposta da equipe responsável pela solicitação.
            if (userLogado is not null && solicitacao.Usuario is not null)
                await _sendEmail.SendEmailMensagemAsync(solicitacao, msgRequest.Conteudo);

            return Result<string>.Success("Mensagem criada com sucesso.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(new ErrorDto(
               Code: ErrorsEnum.DOMAIN_VALIDATION.ToString(),
               Message: ex.Message
           ));
        }
        catch (Exception)
        {
            return Result<string>.Failure(new ErrorDto(
                Code: ErrorsEnum.INTERNAL_ERROR.ToString(),
                Message: "Ocorreu um erro inesperado ao registrar a mensagem."
            ));
        }
    }

    public async Task<Result<IEnumerable<MensagemResponse>>> ObterMensagensPorProtocoloAsync(
        string protocolo, CancellationToken cancellationToken)
    {
        try
        {
            var solicitacao = _solicitacaoRepository
                .ObterSolicitacaoPorProtocoloAsync(protocolo, cancellationToken).Result;

            if (solicitacao is null)
                return Result<IEnumerable<MensagemResponse>>.Failure(new ErrorDto(
                  Code: ErrorsEnum.NOT_FOUND.ToString(),
                  Message: "Solicitação não encontrada."
                ));

            var mensagens =
                await _mensagemRepository.ObterMensagensPorProtocoloAsync(protocolo, cancellationToken);

            if (mensagens is null)
                return Result<IEnumerable<MensagemResponse>>.Failure(new ErrorDto(
                    Code: ErrorsEnum.NOT_FOUND.ToString(),
                    Message: "Nenhuma mensagem encontrada para o protocolo informado."
                ));

            return Result<IEnumerable<MensagemResponse>>
                .Success(mensagens.Select(s => new MensagemResponse
                {
                    Id = s.Id,
                    Conteudo = s.Conteudo,
                    Protocolo = protocolo,
                    PublicadoPeloAutor = s.Autor is null ? true : false,
                    DataMensagem = s.DataCriacao
                }));
        }
        catch (Exception)
        {
            return Result<IEnumerable<MensagemResponse>>.Failure(new ErrorDto(
                Code: ErrorsEnum.INTERNAL_ERROR.ToString(),
                Message: "Ocorreu um erro inesperado ao buscar as mensagens."
            ));
        }
    }
}