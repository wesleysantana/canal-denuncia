using Bogus;
using CanalDenuncias.Domain.Entities;

namespace CanalDenuncias.Tests.Domain.Fixtures;

[CollectionDefinition(nameof(MensagemFixture))]
public class MensagemTestFixtureCollection : ICollectionFixture<MensagemFixture>
{
}

public class MensagemFixture
{
    public Faker _faker { get; } = new("pt_BR");

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

    public Solicitacao CreateValidSolicitacao(bool anonima = true, Usuario? usuario = null)
    {
        var identificador = _faker.Random.String2(10, "abcdefghijklmnopqrstuvwxyz");
        var dataOcorrido = DateTime.Now.AddDays(-1);
        var vinculoId = _faker.Random.Int(1, 99999);

        return new Solicitacao(
            anonima: anonima,
            identificadorColaboradorOuSetor: identificador,
            testemunhas: "Fulano",
            evidencias: "Evidência válida com mais de 5 caracteres",
            usuario: usuario,
            dataOcorrido: dataOcorrido,
            vinculoId: vinculoId
        );
    }

    public Mensagem CreateValidMensagem(
        Solicitacao? solicitacao = null,
        string? conteudo = null,
        string? autor = null)
    {
        var texto = conteudo ?? _faker.Lorem.Paragraphs(2); // normalmente > 25 chars
        while(texto.Length < 25)
            texto += _faker.Lorem.Paragraph(2);

        var sol = solicitacao ?? CreateValidSolicitacao();
        
        var aut = autor ?? _faker.Name.FullName();

        return new Mensagem(texto, sol, aut);
    }

    public string CreateConteudoComTamanho(int length)  => new('x', length);
}