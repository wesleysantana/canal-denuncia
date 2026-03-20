using Bogus;
using CanalDenuncias.Domain.Entities;
using CanalDenuncias.Domain.Repositories;
using Moq;

namespace CanalDenuncias.Tests.Domain.Fixtures;

[CollectionDefinition(nameof(SolicitacaoFixture))]
public class SolicitacaoTestFixtureCollection : ICollectionFixture<SolicitacaoFixture>
{
}

public class SolicitacaoFixture
{
    public Faker _faker { get; } = new("pt_BR");

    public Mock<ISolicitacaoRepository> SolicitacaoRepositoryMock { get; } = new();
    public Mock<IMensagemRepository> MensagemRepositoryMock { get; } = new();
    public Mock<IUnitOfWork> UowMock { get; } = new();

    public Usuario CreateValidUsuario()
    {
        const string cpfValido = "52998224725";

        return new Usuario(
            nome: _faker.Name.FullName(),
            telefone: _faker.Random.ReplaceNumbers("###########"),
            email: _faker.Internet.Email(),
            cPF: cpfValido
        );
    }

    public Solicitacao CreateValidSolicitacao(
        bool anonima = true,
        Usuario? usuario = null,
        DateTime? dataOcorrido = null,
        string? identificador = null,
        string? testemunhas = null,
        string? evidencias = null,
        int? vinculoId = null
    )
    {
        var id = identificador ?? _faker.Random.String2(10, "abcdefghijklmnopqrstuvwxyz");
        var dtOcorrido = dataOcorrido ?? DateTime.Now.AddDays(-1);
        var vId = vinculoId ?? _faker.Random.Int(1, 99999);

        return new Solicitacao(
            anonima: anonima,
            identificadorColaboradorOuSetor: id,
            testemunhas: testemunhas,
            evidencias: evidencias,
            usuario: usuario,
            dataOcorrido: dtOcorrido,
            vinculoId: vId
        );
    }
}