using CanalDenuncias.Application.Services;
using CanalDenuncias.Application.Utlis;
using CanalDenuncias.Domain.Entities;
using CanalDenuncias.Domain.Repositories;
using CanalDenuncias.Infra.EmailService.Services;
using CanalDenuncias.Tests.Application.Fixtures;
using FluentAssertions;
using Moq;

namespace CanalDenuncias.Tests.Application;

[Collection(nameof(MensagemServiceFixture))]
public class MensagemServiceTests
{
    private readonly MensagemServiceFixture _fixture;

    private readonly Mock<ISolicitacaoRepository> _solicitacaoRepository;
    private readonly Mock<IMensagemRepository> _mensagemRepository;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<ISendEmail> _sendEmail;

    private readonly MensagemService _sut;

    public MensagemServiceTests(MensagemServiceFixture fixture)
    {
        _fixture = fixture;

        _solicitacaoRepository = new Mock<ISolicitacaoRepository>(MockBehavior.Strict);
        _mensagemRepository = new Mock<IMensagemRepository>(MockBehavior.Strict);
        _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _sendEmail = new Mock<ISendEmail>(MockBehavior.Strict);

        _sut = new MensagemService(
            _solicitacaoRepository.Object,
            _mensagemRepository.Object,
            _uow.Object,
            _sendEmail.Object);
    }

    #region CriarMensagemAsync

    [Fact]
    public async Task CriarMensagemAsync_QuandoSolicitacaoNaoEncontrada_DeveRetornarNotFound()
    {
        // Arrange
        var ct = CancellationToken.None;
        var request = _fixture.CreateValidMensagemRequest(protocolo: "12345678901");
        var userLogado = "user@teste.com";

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(request.Protocolo, ct))
            .ReturnsAsync((Solicitacao?)null);

        // Act
        var result = await _sut.CriarMensagemAsync(request, userLogado, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.NOT_FOUND.ToString());
        result.Errors[0].Message.Should().Be("Solicitação não encontrada.");

        _mensagemRepository.VerifyNoOtherCalls();
        _uow.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarMensagemAsync_QuandoDadosValidosComUsuarioLogadoESolicitacaoNaoAnonima_DeveEnviarEmail()
    {
        // Arrange
        var ct = CancellationToken.None;
        var protocolo = "12345678901";
        var request = _fixture.CreateValidMensagemRequest(protocolo: protocolo);
        var userLogado = "user@teste.com";

        // Solicitação NÃO anônima (tem usuario)
        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: false, protocolo: protocolo);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        _mensagemRepository
            .Setup(r => r.Add(It.IsAny<Mensagem>()));

        _uow
            .Setup(u => u.CommitAsync(ct))
            .ReturnsAsync(true);

        // Setup para SendEmailMensagemAsync (é chamado porque tem userLogado e usuario)
        _sendEmail
            .Setup(s => s.SendEmailMensagemAsync(solicitacao, request.Conteudo))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CriarMensagemAsync(request, userLogado, ct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Mensagem criada com sucesso.");

        _mensagemRepository.Verify(r => r.Add(It.Is<Mensagem>(m =>
            m.Conteudo == request.Conteudo &&
            m.Solicitacao == solicitacao &&
            m.Autor == userLogado
        )), Times.Once);

        _uow.Verify(u => u.CommitAsync(ct), Times.Once);

        // Verificar que o email foi enviado
        _sendEmail.Verify(s => s.SendEmailMensagemAsync(solicitacao, request.Conteudo), Times.Once);
    }

    [Fact]
    public async Task CriarMensagemAsync_QuandoUserLogadoNulo_NaoDeveEnviarEmail()
    {
        // Arrange
        var ct = CancellationToken.None;
        var protocolo = "12345678901";
        var request = _fixture.CreateValidMensagemRequest(protocolo: protocolo);
        string? userLogado = null; // ✅ Nulo

        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: false, protocolo: protocolo);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        _mensagemRepository
            .Setup(r => r.Add(It.IsAny<Mensagem>()));

        _uow
            .Setup(u => u.CommitAsync(ct))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CriarMensagemAsync(request, userLogado, ct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Mensagem criada com sucesso.");

        _mensagemRepository.Verify(r => r.Add(It.IsAny<Mensagem>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);

        // Email NÃO foi enviado (userLogado é null)
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarMensagemAsync_QuandoSolicitacaoAnonima_NaoDeveEnviarEmail()
    {
        // Arrange
        var ct = CancellationToken.None;
        var protocolo = "12345678901";
        var request = _fixture.CreateValidMensagemRequest(protocolo: protocolo);
        var userLogado = "user@teste.com";

        // Solicitação anônima (usuario é null)
        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: true, protocolo: protocolo);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        _mensagemRepository
            .Setup(r => r.Add(It.IsAny<Mensagem>()));

        _uow
            .Setup(u => u.CommitAsync(ct))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CriarMensagemAsync(request, userLogado, ct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Mensagem criada com sucesso.");

        _mensagemRepository.Verify(r => r.Add(It.IsAny<Mensagem>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);

        // Email NÃO foi enviado (solicitacao.Usuario é null)
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarMensagemAsync_QuandoConteudoInvalido_DeveRetornarDomainValidation()
    {
        // Arrange
        var ct = CancellationToken.None;
        var protocolo = "12345678901";
        var request = _fixture.CreateValidMensagemRequest(protocolo: protocolo, conteudo: "curto"); // < 25
        var userLogado = "user@teste.com";

        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: true, protocolo: protocolo);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        // Act
        var result = await _sut.CriarMensagemAsync(request, userLogado, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.DOMAIN_VALIDATION.ToString());
        result.Errors[0].Message.Should().Be("O conteúdo da mensagem deve conter entre 25 e 2000 caracteres.");

        _mensagemRepository.VerifyNoOtherCalls();
        _uow.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarMensagemAsync_QuandoRepositorioLancaException_DeveRetornarInternalError()
    {
        // Arrange
        var ct = CancellationToken.None;
        var request = _fixture.CreateValidMensagemRequest(protocolo: "12345678901");
        var userLogado = "user@teste.com";

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(request.Protocolo, ct))
            .ThrowsAsync(new Exception("db fail"));

        // Act
        var result = await _sut.CriarMensagemAsync(request, userLogado, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Ocorreu um erro inesperado ao registrar a mensagem.");

        _mensagemRepository.VerifyNoOtherCalls();
        _uow.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarMensagemAsync_QuandoCommitLancaException_DeveRetornarInternalError()
    {
        // Arrange
        var ct = CancellationToken.None;
        var protocolo = "12345678901";
        var request = _fixture.CreateValidMensagemRequest(protocolo: protocolo);
        var userLogado = "user@teste.com";

        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: true, protocolo: protocolo);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        _mensagemRepository
            .Setup(r => r.Add(It.IsAny<Mensagem>()));

        _uow
            .Setup(u => u.CommitAsync(ct))
            .ThrowsAsync(new Exception("commit fail"));

        // Act
        var result = await _sut.CriarMensagemAsync(request, userLogado, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Ocorreu um erro inesperado ao registrar a mensagem.");

        _mensagemRepository.Verify(r => r.Add(It.IsAny<Mensagem>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarMensagemAsync_QuandoSendEmailLancaException_DeveRetornarInternalError()
    {
        // Arrange
        var ct = CancellationToken.None;
        var protocolo = "12345678901";
        var request = _fixture.CreateValidMensagemRequest(protocolo: protocolo);
        var userLogado = "user@teste.com";

        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: false, protocolo: protocolo);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        _mensagemRepository
            .Setup(r => r.Add(It.IsAny<Mensagem>()));

        _uow
            .Setup(u => u.CommitAsync(ct))
            .ReturnsAsync(true);

        // SendEmail lança exception
        _sendEmail
            .Setup(s => s.SendEmailMensagemAsync(solicitacao, request.Conteudo))
            .ThrowsAsync(new Exception("Email service down"));

        // Act
        var result = await _sut.CriarMensagemAsync(request, userLogado, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Ocorreu um erro inesperado ao registrar a mensagem.");

        _mensagemRepository.Verify(r => r.Add(It.IsAny<Mensagem>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);
        _sendEmail.Verify(s => s.SendEmailMensagemAsync(solicitacao, request.Conteudo), Times.Once);
    }

    #endregion CriarMensagemAsync

    #region ObterMensagensPorProtocoloAsync

    [Fact]
    public async Task ObterMensagensPorProtocoloAsync_QuandoSolicitacaoNaoEncontrada_DeveRetornarNotFound()
    {
        // Arrange
        var ct = CancellationToken.None;
        var protocolo = "12345678901";

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync((Solicitacao?)null);

        // Act
        var result = await _sut.ObterMensagensPorProtocoloAsync(protocolo, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.NOT_FOUND.ToString());
        result.Errors[0].Message.Should().Be("Solicitação não encontrada.");

        _mensagemRepository.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ObterMensagensPorProtocoloAsync_QuandoNenhumaMensagemEncontrada_DeveRetornarNotFound()
    {
        // Arrange
        var ct = CancellationToken.None;
        var protocolo = "12345678901";

        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: true, protocolo: protocolo);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        _mensagemRepository
            .Setup(r => r.ObterMensagensPorProtocoloAsync(protocolo, ct))!
            .ReturnsAsync((IEnumerable<Mensagem>?)null);

        // Act
        var result = await _sut.ObterMensagensPorProtocoloAsync(protocolo, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.NOT_FOUND.ToString());
        result.Errors[0].Message.Should().Be("Nenhuma mensagem encontrada para o protocolo informado.");

        _mensagemRepository.Verify(r => r.ObterMensagensPorProtocoloAsync(protocolo, ct), Times.Once);
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ObterMensagensPorProtocoloAsync_QuandoExistemMensagens_DeveMapearResponse()
    {
        // Arrange
        var ct = CancellationToken.None;
        var protocolo = "12345678901";

        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: true, protocolo: protocolo);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        var msgAutorNull = new Mensagem(
            conteudo: _fixture.CreateValidMensagemRequest(protocolo).Conteudo,
            solicitacao: solicitacao,
            autor: null
        );

        var msgAutorNaoNull = new Mensagem(
            conteudo: _fixture.CreateValidMensagemRequest(protocolo).Conteudo,
            solicitacao: solicitacao,
            autor: "user@teste.com"
        );

        _mensagemRepository
            .Setup(r => r.ObterMensagensPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(new List<Mensagem> { msgAutorNull, msgAutorNaoNull });

        // Act
        var result = await _sut.ObterMensagensPorProtocoloAsync(protocolo, ct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(2);

        var list = result.Value!.ToList();
        list[0].Protocolo.Should().Be(protocolo);
        list[0].Conteudo.Should().Be(msgAutorNull.Conteudo);
        list[0].PublicadoPeloAutor.Should().BeTrue(); // Autor null => true

        list[1].Protocolo.Should().Be(protocolo);
        list[1].Conteudo.Should().Be(msgAutorNaoNull.Conteudo);
        list[1].PublicadoPeloAutor.Should().BeFalse(); // Autor != null => false

        _mensagemRepository.Verify(r => r.ObterMensagensPorProtocoloAsync(protocolo, ct), Times.Once);
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ObterMensagensPorProtocoloAsync_QuandoRepositorioLancaException_DeveRetornarInternalError()
    {
        // Arrange
        var ct = CancellationToken.None;
        var protocolo = "12345678901";

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ThrowsAsync(new Exception("fail"));

        // Act
        var result = await _sut.ObterMensagensPorProtocoloAsync(protocolo, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Ocorreu um erro inesperado ao buscar as mensagens.");

        _mensagemRepository.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();
    }

    #endregion ObterMensagensPorProtocoloAsync
}