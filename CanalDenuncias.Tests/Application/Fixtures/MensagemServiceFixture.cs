using Bogus;
using CanalDenuncias.Application.DTOs.Request;
using CanalDenuncias.Domain.Entities;

namespace CanalDenuncias.Tests.Application.Fixtures;

[CollectionDefinition(nameof(MensagemServiceFixture))]
public class MensagemServiceTestFixtureCollection : ICollectionFixture<MensagemServiceFixture>
{
}

public class MensagemServiceFixture
{
    public Faker Faker { get; } = new("pt_BR");

    public const string CpfValido = "11144477735";

    public MensagemRequest CreateValidMensagemRequest(string? protocolo = null, string? conteudo = null)
    {
        var texto = conteudo ?? Faker.Lorem.Paragraphs(2);
        while (conteudo is null && texto.Length < 25)
            texto += " " + Faker.Lorem.Sentence(10);

        return new MensagemRequest
        {
            Protocolo = protocolo ?? Faker.Random.ReplaceNumbers("###########"),
            Conteudo = texto
        };
    }

    public Solicitacao CreateSolicitacaoEntity(bool anonima, string? protocolo = null, int id = 10)
    {
        var usuario = anonima
            ? null
            : new Usuario(
                Faker.Name.FullName(),
                Faker.Phone.PhoneNumber("###########"),
                Faker.Internet.Email(),
                CpfValido
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
       
        if (!string.IsNullOrWhiteSpace(protocolo))
            SetPrivateProperty(solicitacao, nameof(Solicitacao.Protocolo), protocolo);

        return solicitacao;
    }

    private static void SetPrivateProperty<T>(T instance, string propertyName, object? value)
    {
        var prop = typeof(T).GetProperty(propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (prop is null)
            throw new InvalidOperationException($"Property '{propertyName}' não encontrada em {typeof(T).Name}");

        prop.SetValue(instance, value);
    }
}