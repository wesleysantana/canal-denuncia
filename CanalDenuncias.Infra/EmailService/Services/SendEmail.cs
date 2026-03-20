using System.Net;
using System.Net.Mail;
using System.Reflection;
using CanalDenuncias.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CanalDenuncias.Infra.EmailService.Services;

public class SendEmail : ISendEmail
{
    private readonly IConfiguration _config;

    private const string ASSUNTO = "Canal de Denúncias - Unimed Prudente";
    private const string EMAILSOLICITACAO_TEMPLATE = "EmailSolicitacao";
    private const string EMAILSTATUS_TEMPLATE = "EmailStatus";
    private const string EMAILMENSAGEM_TEMPLATE = "EmailMensagem";

    private string _remetente = string.Empty;
    private string _destinatario = string.Empty;
    private string _credential = string.Empty;

    public SendEmail(IConfiguration config)
    {
        _config = config;
        SeedConfiguration();
    }

    private void SeedConfiguration()
    {
        _remetente = _config["Emails:rementente"] ?? string.Empty;
        _destinatario = _config["Emails:destinatario"] ?? string.Empty;
        _credential = _config["Emails:credential"] ?? string.Empty;

        if (string.IsNullOrEmpty(_remetente) 
            || string.IsNullOrEmpty(_destinatario) 
            || string.IsNullOrEmpty(_credential))
        {
            var msg = @"Configurações de email ausentes:
                credencial, remetente ou destinatário não configurados.";

            ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SendEmail>();
            logger.LogError(msg);

            throw new Exception(msg);
        }
    }

    public async Task SendEmailSolicitacaoAsync(string protocolo, string? emailUsuario)
    {
        try
        {
            string corpoHtml = await GetTemplateHtml(EMAILSOLICITACAO_TEMPLATE);

            corpoHtml = corpoHtml
                .Replace("{{protocolo}}", protocolo)
                .Replace("{{ano}}", DateTime.Now.Year.ToString());

            await EnvioEmail(corpoHtml, emailUsuario ?? string.Empty);
        }
        catch (Exception ex)
        {
            ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SendEmail>();
            logger.LogError(ex, "Erro ao enviar email: {Message}", ex.Message);
        }
    }

    public async Task SendEmailStatusChangeAsync(Solicitacao solicitacao)
    {
        if (solicitacao.Usuario?.Email is null)
        {
            ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SendEmail>();
            logger.LogWarning("Solicitação {Protocolo} não possui email de usuário para envio de status.",
                solicitacao.Protocolo);
            return;
        }

        _destinatario = solicitacao.Usuario.Email;
        try
        {
            string corpoHtml = await GetTemplateHtml(EMAILSTATUS_TEMPLATE);

            corpoHtml = corpoHtml
                .Replace("{{protocolo}}", solicitacao.Protocolo)
                .Replace("{{status}}", solicitacao.StatusSolicitacao!.Descricao)
                .Replace("{{ano}}", DateTime.Now.Year.ToString());

            await EnvioEmail(corpoHtml);
        }
        catch (Exception ex)
        {
            ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SendEmail>();
            logger.LogError(ex, "Erro ao enviar email: {Message}", ex.Message);
        }
    }

    public async Task SendEmailMensagemAsync(Solicitacao solicitacao, string mensagem)
    {
        if (solicitacao.Usuario?.Email is null)
        {
            ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SendEmail>();
            logger.LogWarning("Solicitação {Protocolo} não possui email de usuário para envio de status.",
                solicitacao.Protocolo);
            return;
        }
        _destinatario = solicitacao.Usuario.Email;
        try
        {
            string corpoHtml = await GetTemplateHtml(EMAILMENSAGEM_TEMPLATE);

            corpoHtml = corpoHtml
                .Replace("{{protocolo}}", solicitacao.Protocolo)
                .Replace("{{mensagem}}", mensagem)
                .Replace("{{ano}}", DateTime.Now.Year.ToString());

            await EnvioEmail(corpoHtml);
        }
        catch (Exception ex)
        {
            ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SendEmail>();
            logger.LogError(ex, "Erro ao enviar email: {Message}", ex.Message);
        }
    }

    private async Task EnvioEmail(string corpoHtml, string destinatario2 = "")
    {
        var mailRemetente = new MailAddress(_remetente);

        using MailMessage email = new()
        {
            From = mailRemetente,
            Subject = ASSUNTO,
            Body = corpoHtml,
            IsBodyHtml = true
        };

        email.To.Add(new MailAddress(_destinatario));
        if (destinatario2 != string.Empty) email.To.Add(new MailAddress(destinatario2.Trim()));

        using SmtpClient client = new()
        {
            Host = "smtp.sendgrid.net",
            Port = 587,
            Credentials = new NetworkCredential("apikey", _credential),
            EnableSsl = true
        };

        await client.SendMailAsync(email);
    }

    private async Task<string> GetTemplateHtml(string origem)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // O nome do recurso é composto pelo namespace + nome da pasta + nome do arquivo
        var resourceName = $"{typeof(SendEmail).Assembly.GetName().Name}.EmailService.Templates.{origem}.html";

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync();
    }
}