using Bogus;
using CanalDenuncias.Domain.Exceptions;
using CanalDenuncias.Tests.Domain.Fixtures;
using FluentAssertions;

namespace CanalDenuncias.Domain.Entities;

[Collection(nameof(SolicitacaoFixture))]
public class SolicitacaoTests
{
    private readonly SolicitacaoFixture _fixture;

    public SolicitacaoTests(SolicitacaoFixture fixture)
        => _fixture = fixture;

    [Fact]
    public void Ctor_Deve_Criar_Solicitacao_Anonima_Valida_Quando_Dados_Validos()
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao(
            anonima: true,
            usuario: null,
            testemunhas: "Fulano, Cicrano",
            evidencias: "Prints e e-mails relevantes",
            identificador: "setor-ti",
            vinculoId: 10
        );

        // Assert
        solicitacao.Anonima.Should().BeTrue();
        solicitacao.Usuario.Should().BeNull();
        solicitacao.IdentificadorColaboradorOuSetor.Should().Be("setor-ti");
        solicitacao.Testemunhas.Should().Be("Fulano, Cicrano");
        solicitacao.Evidencias.Should().Be("Prints e e-mails relevantes");
        solicitacao.VinculoId.Should().Be(10);
        solicitacao.StatusSolicitacaoId.Should().Be(1);

        solicitacao.Protocolo.Should().NotBeNullOrWhiteSpace();
        solicitacao.Protocolo.Length.Should().Be(11); // "yyMMmm" (6) + 5 dígitos
        solicitacao.Protocolo.Should().MatchRegex(@"^\d{11}$");
    }

    [Fact]
    public void Ctor_Deve_Criar_Solicitacao_NaoAnonima_Valida_Quando_Usuario_Presente()
    {
        // Arrange
        var usuario = _fixture.CreateValidUsuario();

        // Act
        var solicitacao = _fixture.CreateValidSolicitacao(
            anonima: false,
            usuario: usuario,
            testemunhas: "Beltrano",
            evidencias: "Vídeo do ocorrido",
            identificador: "colab-123"
        );

        // Assert
        solicitacao.Anonima.Should().BeFalse();
        solicitacao.Usuario.Should().NotBeNull();
        solicitacao.Usuario.Should().BeSameAs(usuario);
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Identificador_Tiver_Menos_De_3()
    {
        // Arrange
        var identificadorInvalido = "ab";

        // Act
        var act = () => _fixture.CreateValidSolicitacao(identificador: identificadorInvalido);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("O identificador do colaborador ou setor deve conter entre 3 e 100 caracteres.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Identificador_Tiver_Mais_De_100()
    {
        // Arrange
        var identificadorInvalido = new string('a', 101);

        // Act
        var act = () => _fixture.CreateValidSolicitacao(identificador: identificadorInvalido);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("O identificador do colaborador ou setor deve conter entre 3 e 100 caracteres.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Testemunhas_Tiver_Menos_De_3()
    {
        // Arrange
        var testemunhasInvalidas = "ab";

        // Act
        var act = () => _fixture.CreateValidSolicitacao(testemunhas: testemunhasInvalidas);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("As testemunhas devem conter entre 3 e 500 caracteres.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Testemunhas_Tiver_Mais_De_500()
    {
        // Arrange
        var testemunhasInvalidas = new string('x', 501);

        // Act
        var act = () => _fixture.CreateValidSolicitacao(testemunhas: testemunhasInvalidas);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("As testemunhas devem conter entre 3 e 500 caracteres.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Evidencias_Tiver_Menos_De_5()
    {
        // Arrange
        var evidenciasInvalidas = "abcd";

        // Act
        var act = () => _fixture.CreateValidSolicitacao(evidencias: evidenciasInvalidas);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("As evidências devem conter entre 5 e 500 caracteres.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Evidencias_Tiver_Mais_De_500()
    {
        // Arrange
        var evidenciasInvalidas = new string('e', 501);

        // Act
        var act = () => _fixture.CreateValidSolicitacao(evidencias: evidenciasInvalidas);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("As evidências devem conter entre 5 e 500 caracteres.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Anonima_For_True_E_Usuario_For_Preenchido()
    {
        // Arrange
        var usuario = _fixture.CreateValidUsuario();

        // Act
        var act = () => _fixture.CreateValidSolicitacao(anonima: true, usuario: usuario);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Uma solicitação anônima não pode ter um usuário associado.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Anonima_For_False_E_Usuario_For_Null()
    {
        // Act
        var act = () => _fixture.CreateValidSolicitacao(anonima: false, usuario: null);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Uma solicitação não anônima deve ter um usuário associado.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_DataOcorrencia_For_No_Futuro()
    {
        // Arrange
        var dataFutura = DateTime.Now.AddMinutes(1);

        // Act
        var act = () => _fixture.CreateValidSolicitacao(dataOcorrido: dataFutura);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("A data do ocorrido não pode ser no futuro.");
    }

    [Fact]
    public void AtualizarStatus_Deve_Alterar_StatusSolicitacaoId_Quando_Id_Valido()
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao();
        var novoStatusId = 2;

        // Act
        solicitacao.AtualizarStatus(novoStatusId);

        // Assert
        solicitacao.StatusSolicitacaoId.Should().Be(novoStatusId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void AtualizarStatus_Deve_Lancar_DomainException_Quando_Id_Menor_Que_1(int statusIdInvalido)
    {
        // Arrange
        var solicitacao = _fixture.CreateValidSolicitacao();

        // Act
        var act = () => solicitacao.AtualizarStatus(statusIdInvalido);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("O ID do status da solicitação deve ser maior que zero.");
    }
}