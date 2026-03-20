using CanalDenuncias.Domain.Exceptions;
using CanalDenuncias.Tests.Domain.Fixtures;
using FluentAssertions;

namespace CanalDenuncias.Domain.Entities;

[Collection(nameof(MensagemFixture))]
public class MensagemTests
{
    private readonly MensagemFixture _fixture;

    public MensagemTests(MensagemFixture fixture) => _fixture = fixture;

    [Fact]
    public void Ctor_Deve_Criar_Mensagem_Valida_Quando_Dados_Validos()
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao();
        var conteudo = _fixture.CreateConteudoComTamanho(50);
        var autor = "Rita";

        // Act
        var mensagem = new Mensagem(conteudo, solicitacao, autor);

        // Assert
        mensagem.Conteudo.Should().Be(conteudo);
        mensagem.Solicitacao.Should().BeSameAs(solicitacao);
        mensagem.Autor.Should().Be(autor);
    }

    [Fact]
    public void Ctor_Deve_Aceitar_Autor_Null()
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao();
        var conteudo = _fixture.CreateConteudoComTamanho(60);

        // Act
        var mensagem = new Mensagem(conteudo, solicitacao, autor: null);

        // Assert
        mensagem.Autor.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Ctor_Deve_Lancar_DomainException_Quando_Conteudo_For_Null_Ou_Vazio(string? conteudo)
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao();

        // Act
        var act = () => new Mensagem(conteudo!, solicitacao, autor: "Autor");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O conteúdo da mensagem não pode ser nulo ou vazio.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Conteudo_For_Apenas_Espacos()
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao();
        var conteudo = "     ";

        // Act
        var act = () => new Mensagem(conteudo, solicitacao, autor: "Autor");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O conteúdo da mensagem não pode ser nulo ou vazio.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Conteudo_Tiver_Menos_De_25()
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao();
        var conteudo = _fixture.CreateConteudoComTamanho(24);

        // Act
        var act = () => new Mensagem(conteudo, solicitacao, autor: "Autor");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O conteúdo da mensagem deve conter entre 25 e 2000 caracteres.");
    }

    [Fact]
    public void Ctor_Deve_Criar_Mensagem_Quando_Conteudo_Tiver_Exatamente_25()
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao();
        var conteudo = _fixture.CreateConteudoComTamanho(25);

        // Act
        var mensagem = new Mensagem(conteudo, solicitacao, autor: "Autor");

        // Assert
        mensagem.Conteudo.Should().HaveLength(25);
    }

    [Fact]
    public void Ctor_Deve_Criar_Mensagem_Quando_Conteudo_Tiver_Exatamente_2000()
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao();
        var conteudo = _fixture.CreateConteudoComTamanho(2000);

        // Act
        var mensagem = new Mensagem(conteudo, solicitacao, autor: "Autor");

        // Assert
        mensagem.Conteudo.Should().HaveLength(2000);
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Conteudo_Tiver_Mais_De_2000()
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao();
        var conteudo = _fixture.CreateConteudoComTamanho(2001);

        // Act
        var act = () => new Mensagem(conteudo, solicitacao, autor: "Autor");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O conteúdo da mensagem deve conter entre 25 e 2000 caracteres.");
    }

    [Fact]
    public void Propriedade_SolicitacaoId_Deve_Ser_Setavel()
    {
        // Arrange
        var mensagem = _fixture.CreateValidMensagem();
        var id = 123;

        // Act
        mensagem.SolicitacaoId = id;

        // Assert
        mensagem.SolicitacaoId.Should().Be(id);
    }

    [Fact]
    public void Propriedade_Autor_Deve_Ser_Setavel()
    {
        // Arrange
        var mensagem = _fixture.CreateValidMensagem();
        mensagem.Autor.Should().NotBeNull();

        // Act
        mensagem.Autor = "Novo Autor";

        // Assert
        mensagem.Autor.Should().Be("Novo Autor");
    }
}