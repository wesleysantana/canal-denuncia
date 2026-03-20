using CanalDenuncias.Domain.Entities;

namespace CanalDenuncias.Infra.EmailService.Services;

public interface ISendEmail
{
    Task SendEmailSolicitacaoAsync(string mensagem, string emailUsuario);

    Task SendEmailStatusChangeAsync(Solicitacao solicitacao);

    Task SendEmailMensagemAsync(Solicitacao solicitacao, string mensagem);
}