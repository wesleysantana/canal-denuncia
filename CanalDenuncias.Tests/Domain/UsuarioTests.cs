using CanalDenuncias.Domain.Exceptions;
using CanalDenuncias.Tests.Domain.Fixtures;
using FluentAssertions;

namespace CanalDenuncias.Domain.Entities;

[Collection(nameof(UsuarioFixture))]
public class UsuarioTests
{
    private readonly UsuarioFixture _fixture;

    public UsuarioTests(UsuarioFixture fixture) => _fixture = fixture;

    [Fact]
    public void Ctor_Deve_Criar_Usuario_Valido_Quando_Dados_Validos()
    {
        // Arrange
        var nome = "Maria da Silva";
        var telefone = "83999998888";
        var email = "maria@email.com";
        var cpf = UsuarioFixture.CpfValido;

        // Act
        var usuario = _fixture.CreateValidUsuario(nome, telefone, email, cpf);

        // Assert
        usuario.Nome.Should().Be(nome);
        usuario.Telefone.Should().Be(telefone);
        usuario.Email.Should().Be(email);
        usuario.CPF.Should().Be(cpf);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Ctor_Deve_Lancar_DomainException_Quando_Nome_For_Null_Ou_Vazio(string? nome)
    {
        // Arrange
        var telefone = _fixture._faker.Random.ReplaceNumbers("###########");
        var email = _fixture._faker.Internet.Email();
        var cpf = UsuarioFixture.CpfValido;

        // Act
        var act = () => new Usuario(
            nome: nome!,
            telefone: telefone,
            email: email,
            cPF: cpf
        );

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O nome não pode ser vazio");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Nome_For_Apenas_Espacos()
    {
        // Arrange
        var telefone = _fixture._faker.Random.ReplaceNumbers("###########");
        var email = _fixture._faker.Internet.Email();
        var cpf = UsuarioFixture.CpfValido;

        // Act
        var act = () => new Usuario(
            nome: "   ",
            telefone: telefone,
            email: email,
            cPF: cpf
        );

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O nome não pode ser vazio");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Nome_Tiver_Menos_De_3()
    {
        // Arrange
        var nomeInvalido = "ab";

        // Act
        var act = () => _fixture.CreateValidUsuario(nome: nomeInvalido);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O nome deve conter entre 3 e 100 caracteres.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Nome_Tiver_Mais_De_100()
    {
        // Arrange
        var nomeInvalido = _fixture.CreateStringComTamanho(101, 'n');

        // Act
        var act = () => _fixture.CreateValidUsuario(nome: nomeInvalido);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O nome deve conter entre 3 e 100 caracteres.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Ctor_Deve_Lancar_DomainException_Quando_Telefone_For_Null_Ou_Vazio(string? telefone)
    {
        // Arrange
        var nome = _fixture._faker.Name.FullName();
        var email = _fixture._faker.Internet.Email();
        var cpf = UsuarioFixture.CpfValido;

        // Act
        var act = () => new Usuario(
            nome: nome,
            telefone: telefone!,
            email: email,
            cPF: cpf
        );

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O telefone não pode ser vazio.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Telefone_For_Apenas_Espacos()
    {
        // Act
        var act = () => _fixture.CreateValidUsuario(telefone: "   ");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O telefone não pode ser vazio.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Telefone_Tiver_Menos_De_8()
    {
        // Arrange
        var telefoneInvalido = _fixture.CreateStringComTamanho(7, '1');

        // Act
        var act = () => _fixture.CreateValidUsuario(telefone: telefoneInvalido);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O telefone deve conter entre 8 e 15 caracteres.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Telefone_Tiver_Mais_De_15()
    {
        // Arrange
        var telefoneInvalido = _fixture.CreateStringComTamanho(16, '1');

        // Act
        var act = () => _fixture.CreateValidUsuario(telefone: telefoneInvalido);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O telefone deve conter entre 8 e 15 caracteres.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_Email_For_Invalido()
    {
        // Arrange
        var emailInvalido = "email-invalido";

        // Act
        var act = () => _fixture.CreateValidUsuario(email: emailInvalido);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O email é inválido.");
    }

    [Fact]
    public void Ctor_Deve_Lancar_DomainException_Quando_CPF_For_Invalido()
    {
        // Arrange
        var cpfInvalido = "11111111111";

        // Act
        var act = () => _fixture.CreateValidUsuario(cpf: cpfInvalido);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("O CPF é inválido.");
    }
}