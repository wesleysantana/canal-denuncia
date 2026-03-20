using CanalDenuncias.Application.DTOs.Request;
using CanalDenuncias.Application.Interfaces;
using CanalDenuncias.Application.Services;
using CanalDenuncias.Domain.Repositories;
using CanalDenuncias.Infra.Data.Repositories;
using CanalDenuncias.Infra.EmailService.Services;
using CanalDenuncias.Infra.FileStorage.Services;
using FluentValidation;

namespace CanalDenuncias.API.Bootstrap;

public class IoC
{
    public static void Register(IServiceCollection service)
    {
        // Validators
        service.AddScoped<IValidator<SolicitacaoRequest>, SolicitacaoRequestValidator>();

        // Repositories
        service.AddScoped<ISolicitacaoRepository, SolicitacaoRepository>();
        service.AddScoped<IMensagemRepository, MensagemRepository>();
        service.AddScoped<IUnitOfWork, UnitOfWork>();
        service.AddScoped<ISendEmail, SendEmail>();
        service.AddScoped<ISolicitacaoAnexoRepository, SolicitacaoAnexoRepository>();

        // Services
        service.AddScoped<ISolicitacaoService, SolicitacaoService>();
        service.AddScoped<IMensagemService, MensagemService>();

        // Storage local (share)
        service.AddScoped<IAnexoStorageService, AnexoStorageService>();
    }
}