using System.ComponentModel.DataAnnotations;
using CanalDenuncias.Application.Utlis;
using CanalDenuncias.Domain.Utils;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace CanalDenuncias.Application.DTOs.Request;

public class SolicitacaoRequest
{
    [Required(ErrorMessage = MessagesDataAnnotations.Required)]
    public bool Anonimo { get; set; }

    [Required(ErrorMessage = MessagesDataAnnotations.Required)]
    public DateTime DataOcorrido { get; set; }

    [MaxLength(100, ErrorMessage = MessagesDataAnnotations.MaxLength)]
    public string? IdentificadorSetorOuPessoa { get; set; }

    [MaxLength(500, ErrorMessage = MessagesDataAnnotations.MaxLength)]
    public string? Testemunhas { get; set; }

    [MaxLength(500, ErrorMessage = MessagesDataAnnotations.MaxLength)]
    public string? Evidencias { get; set; }

    [Required(ErrorMessage = MessagesDataAnnotations.Required)]
    [StringLength(2000, MinimumLength = 25, ErrorMessage = MessagesDataAnnotations.StringLength)]
    public string Mensagem { get; set; } = string.Empty;

    public List<IFormFile>? Anexos { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public int? Vinculo { get; set; }
}

public class SolicitacaoRequestValidator : AbstractValidator<SolicitacaoRequest>
{
    public SolicitacaoRequestValidator()
    {
        When(x => x.Anonimo == false, () =>
        {
            RuleFor(x => x.Vinculo)
                .NotNull().WithMessage("O vínculo é obrigatório para denúncias não anônimas.");

            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome não pode ser vazio.")
                .Length(3, 100).WithMessage("O nome deve conter entre 3 e 100 caracteres.");

            RuleFor(x => x.Telefone)
                .NotEmpty().WithMessage("O telefone não pode ser vazio.")
                .Length(8, 15).WithMessage("O telefone deve conter entre 8 e 15 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email não pode ser vazio.")
                .Must(DomainValidator.IsValidEmail).WithMessage("O email é inválido.");

            RuleFor(x => x.Cpf)
                .NotEmpty().WithMessage("O CPF não pode ser vazio.")
                .Must(DomainValidator.CPFValidator).WithMessage("O CPF é inválido.");
        });
    }
}