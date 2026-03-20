using System.Reflection;
using Bogus;
using CanalDenuncias.Application.DTOs.Request;
using CanalDenuncias.Domain.Entities;

namespace CanalDenuncias.Tests.Application.Fixtures;

[CollectionDefinition(nameof(SolicitacaoServiceFixture))]
public class SolicitacaoServiceTestFixtureCollection : ICollectionFixture<SolicitacaoServiceFixture>
{
}

public class SolicitacaoServiceFixture
{
    public Faker Faker { get; } = new("pt_BR");

    public SolicitacaoRequest CreateValidRequest(bool anonimo)
    {
        var mensagem = Faker.Lorem.Paragraphs(2);
        while (mensagem.Length < 25)
            mensagem += " " + Faker.Lorem.Paragraph();

        return new SolicitacaoRequest
        {
            Anonimo = anonimo,
            Nome = (anonimo ? null : Faker.Name.FullName())!,
            Telefone = (anonimo ? null : Faker.Phone.PhoneNumber("###########"))!,
            Email = (anonimo ? null : Faker.Internet.Email())!,
            Cpf = (anonimo ? null : "11144477735")!, // CPF válido

            IdentificadorSetorOuPessoa = Faker.Random.String2(5, 30),
            Testemunhas = Faker.Lorem.Sentence(8),
            Evidencias = Faker.Lorem.Sentence(12),
            DataOcorrido = DateTime.Now.AddDays(-2),
            Vinculo = Faker.Random.Int(1, 9999),

            Mensagem = mensagem
        };
    }

    public Solicitacao CreateSolicitacaoEntity(
        bool anonima,
        string? statusDescricao = "Em análise",
        int id = 10)
    {
        var usuario = anonima
            ? null
            : new Usuario(
                Faker.Name.FullName(),
                Faker.Phone.PhoneNumber("###########"),
                Faker.Internet.Email(),
                "11144477735"
            );

        var solicitacao = new Solicitacao(
            anonima: anonima,
            identificadorColaboradorOuSetor: Faker.Random.String2(5, 30),
            testemunhas: Faker.Lorem.Sentence(8),
            evidencias: Faker.Lorem.Sentence(12),
            usuario: usuario,
            dataOcorrido: DateTime.Now.AddDays(-1),
            vinculoId: Faker.Random.Int(1, 9999)
        );

        SetPrivateProperty(solicitacao, nameof(Solicitacao.Id), id);

        var status = new StatusSolicitacao();
        SetPrivateProperty(status, nameof(StatusSolicitacao.Descricao), statusDescricao ?? "Em análise");
        SetPrivateProperty(status, nameof(StatusSolicitacao.Id), 1);
        SetPrivateProperty(solicitacao, nameof(Solicitacao.StatusSolicitacao), status);

        return solicitacao;
    }

    private static void SetPrivateProperty<T>(T instance, string propertyName, object? value)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop is null)
            throw new InvalidOperationException($"Property '{propertyName}' não encontrada em {typeof(T).Name}");

        prop.SetValue(instance, value);
    }
}