using CanalDenuncias.Application.Services;
using CanalDenuncias.Application.Utlis;
using CanalDenuncias.Domain.Entities;
using CanalDenuncias.Domain.Filters;
using CanalDenuncias.Domain.Repositories;
using CanalDenuncias.Infra.EmailService.Services;
using CanalDenuncias.Infra.FileStorage.Services;
using CanalDenuncias.Tests.Application.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CanalDenuncias.Tests.Application;

[Collection(nameof(SolicitacaoServiceFixture))]
public class SolicitacaoServiceTests
{
    private readonly SolicitacaoServiceFixture _fixture;

    private readonly Mock<ISolicitacaoRepository> _solicitacaoRepository;
    private readonly Mock<IMensagemRepository> _mensagemRepository;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IAnexoStorageService> _storage;
    private readonly Mock<ISolicitacaoAnexoRepository> _solicitacaoAnexoRepository;
    private readonly Mock<ILogger<SolicitacaoService>> _logger;
    private readonly Mock<ISendEmail> _sendEmail;

    private readonly SolicitacaoService _sut;

    public SolicitacaoServiceTests(SolicitacaoServiceFixture fixture)
    {
        _fixture = fixture;

        _solicitacaoRepository = new Mock<ISolicitacaoRepository>(MockBehavior.Strict);
        _mensagemRepository = new Mock<IMensagemRepository>(MockBehavior.Strict);
        _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _storage = new Mock<IAnexoStorageService>(MockBehavior.Strict);
        _logger = new Mock<ILogger<SolicitacaoService>>(MockBehavior.Loose);
        _sendEmail = new Mock<ISendEmail>(MockBehavior.Strict);
        _solicitacaoAnexoRepository = new Mock<ISolicitacaoAnexoRepository>(MockBehavior.Strict);

        _sut = new SolicitacaoService(
            _solicitacaoRepository.Object,
            _mensagemRepository.Object,
            _uow.Object,
            _sendEmail.Object,
            _storage.Object,
            _solicitacaoAnexoRepository.Object,
            _logger.Object);
    }

    #region CriarSolicitacaoAsync Tests

    [Fact]
    public async Task CriarSolicitacaoAsync_QuandoSolicitacaoAnonimaSemAnexos_DeveRetornarSucesso()
    {
        // Arrange
        var request = _fixture.CreateValidRequest(anonimo: true);
        var ct = CancellationToken.None;

        _solicitacaoRepository.Setup(r => r.Add(It.IsAny<Solicitacao>()));
        _mensagemRepository.Setup(r => r.Add(It.IsAny<Mensagem>()));
        _uow.Setup(u => u.CommitAsync(ct)).ReturnsAsync(true);

        // Service chama SendEmailAsync(protocolo, usuario?.Email!) => email null para anônimo
        _sendEmail.Setup(s => s.SendEmailSolicitacaoAsync(It.IsAny<string>(), null!))
                  .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CriarSolicitacaoAsync(request, anexos: null, ct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
        result.Value.Should().HaveLength(11);

        _solicitacaoRepository.Verify(r => r.Add(It.IsAny<Solicitacao>()), Times.Once);
        _mensagemRepository.Verify(r => r.Add(It.IsAny<Mensagem>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);
        _sendEmail.Verify(s => s.SendEmailSolicitacaoAsync(It.IsAny<string>(), null!), Times.Once);

        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_QuandoSolicitacaoNaoAnonimaSemAnexos_DeveRetornarSucesso()
    {
        // Arrange
        var request = _fixture.CreateValidRequest(anonimo: false);
        var ct = CancellationToken.None;

        _solicitacaoRepository.Setup(r => r.Add(It.IsAny<Solicitacao>()));
        _mensagemRepository.Setup(r => r.Add(It.IsAny<Mensagem>()));
        _uow.Setup(u => u.CommitAsync(ct)).ReturnsAsync(true);

        _sendEmail.Setup(s => s.SendEmailSolicitacaoAsync(It.IsAny<string>(), request.Email))
                  .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CriarSolicitacaoAsync(request, anexos: null, ct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();

        _solicitacaoRepository.Verify(r => r.Add(It.IsAny<Solicitacao>()), Times.Once);
        _mensagemRepository.Verify(r => r.Add(It.IsAny<Mensagem>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);
        _sendEmail.Verify(s => s.SendEmailSolicitacaoAsync(It.IsAny<string>(), request.Email), Times.Once);

        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_QuandoNumeroAnexosExcede5_DeveRetornarValidationError()
    {
        // Arrange
        var request = _fixture.CreateValidRequest(anonimo: true);
        var ct = CancellationToken.None;

        var anexos = new List<IFormFile>();
        for (int i = 0; i < 6; i++)
        {
            var f = new Mock<IFormFile>();
            f.SetupGet(x => x.Length).Returns(1_000_000);
            f.SetupGet(x => x.FileName).Returns($"f{i}.bin");
            f.SetupGet(x => x.ContentType).Returns("application/octet-stream");
            anexos.Add(f.Object);
        }

        // Act
        var result = await _sut.CriarSolicitacaoAsync(request, anexos, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.VALIDATION.ToString());
        result.Errors[0].Message.Should().Be("Máximo de 5 anexos permitido.");

        _solicitacaoRepository.VerifyNoOtherCalls();
        _mensagemRepository.VerifyNoOtherCalls();
        _uow.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_QuandoAnexoExcede10MB_DeveRetornarValidationError()
    {
        // Arrange
        var request = _fixture.CreateValidRequest(anonimo: true);
        var ct = CancellationToken.None;

        var mockFile = new Mock<IFormFile>();
        mockFile.SetupGet(f => f.Length).Returns(11 * 1024 * 1024); // 11MB
        mockFile.SetupGet(f => f.FileName).Returns("big.bin");
        mockFile.SetupGet(f => f.ContentType).Returns("application/octet-stream");

        var anexos = new List<IFormFile> { mockFile.Object };

        // Act
        var result = await _sut.CriarSolicitacaoAsync(request, anexos, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("VALIDATION");
        result.Errors[0].Message.Should().Be("Cada anexo deve ter no máximo 10MB.");

        _solicitacaoRepository.VerifyNoOtherCalls();
        _mensagemRepository.VerifyNoOtherCalls();
        _uow.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_QuandoUploadAnexoFalha_DeveRetornarUploadError()
    {
        // Arrange
        var request = _fixture.CreateValidRequest(anonimo: true);
        var ct = CancellationToken.None;

        var mockFile = new Mock<IFormFile>();
        mockFile.SetupGet(f => f.Length).Returns(5 * 1024 * 1024); // 5MB
        mockFile.SetupGet(f => f.FileName).Returns("a.pdf");
        mockFile.SetupGet(f => f.ContentType).Returns("application/pdf");

        var anexos = new List<IFormFile> { mockFile.Object };

        _storage
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), ct))
            .ThrowsAsync(new IOException("Upload failed"));

        // Act
        var result = await _sut.CriarSolicitacaoAsync(request, anexos, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.UPLOAD_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Erro ao enviar anexos.");

        _solicitacaoRepository.VerifyNoOtherCalls();
        _mensagemRepository.VerifyNoOtherCalls();
        _uow.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();

        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), ct), Times.Once);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_QuandoValidacaoDominioFalha_DeveRetornarDomainValidationError()
    {
        // Arrange
        var request = _fixture.CreateValidRequest(anonimo: false);
        request.Nome = ""; // Força DomainException no Usuario

        var ct = CancellationToken.None;

        // Act
        var result = await _sut.CriarSolicitacaoAsync(request, anexos: null, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.DOMAIN_VALIDATION.ToString());

        _solicitacaoRepository.VerifyNoOtherCalls();
        _mensagemRepository.VerifyNoOtherCalls();
        _uow.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_QuandoCommitFalha_DeveRetornarInternalError()
    {
        // Arrange
        var request = _fixture.CreateValidRequest(anonimo: true);
        var ct = CancellationToken.None;

        _solicitacaoRepository.Setup(r => r.Add(It.IsAny<Solicitacao>()));
        _mensagemRepository.Setup(r => r.Add(It.IsAny<Mensagem>()));

        _uow.Setup(u => u.CommitAsync(ct))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.CriarSolicitacaoAsync(request, anexos: null, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Ocorreu um erro inesperado ao registrar a solicitação.");

        _solicitacaoRepository.Verify(r => r.Add(It.IsAny<Solicitacao>()), Times.Once);
        _mensagemRepository.Verify(r => r.Add(It.IsAny<Mensagem>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);

        _sendEmail.VerifyNoOtherCalls();
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_QuandoUploadSucessoMasCommitFalha_DeveCompensarDeletandoUpload()
    {
        // Arrange
        var request = _fixture.CreateValidRequest(anonimo: true);
        var ct = CancellationToken.None;

        var mockFile = new Mock<IFormFile>();
        mockFile.SetupGet(f => f.Length).Returns(5 * 1024 * 1024);
        mockFile.SetupGet(f => f.FileName).Returns("img.jpg");
        mockFile.SetupGet(f => f.ContentType).Returns("image/jpeg");

        var anexos = new List<IFormFile> { mockFile.Object };

        _solicitacaoRepository.Setup(r => r.Add(It.IsAny<Solicitacao>()));
        _mensagemRepository.Setup(r => r.Add(It.IsAny<Mensagem>()));

        _storage.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), ct))
                .ReturnsAsync("nome_do_arquivo_mockado.jpg");

        // Após upload/mapear, o service chama _anexoStorage.Add(a)
        _solicitacaoAnexoRepository.Setup(s => s.Add(It.IsAny<SolicitacaoAnexo>()));

        _uow.Setup(u => u.CommitAsync(ct))
            .ThrowsAsync(new Exception("Database error"));

        _storage.Setup(s => s.DeleteByProtocoloAsync(It.IsAny<string>(), ct))
                .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CriarSolicitacaoAsync(request, anexos, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());

        _solicitacaoRepository.Verify(r => r.Add(It.IsAny<Solicitacao>()), Times.Once);
        _mensagemRepository.Verify(r => r.Add(It.IsAny<Mensagem>()), Times.Once);

        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), ct), Times.Once);
        _solicitacaoAnexoRepository.Verify(s => s.Add(It.IsAny<SolicitacaoAnexo>()), Times.Once);

        _uow.Verify(u => u.CommitAsync(ct), Times.Once);

        _storage.Verify(s => s.DeleteByProtocoloAsync(It.IsAny<string>(), ct), Times.Once);

        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_QuandoCompensacaoUploadFalha_DeveLogarErroMasRetornarResultadoOriginal()
    {
        // Arrange
        var request = _fixture.CreateValidRequest(anonimo: true);
        var ct = CancellationToken.None;

        var mockFile = new Mock<IFormFile>();
        mockFile.SetupGet(f => f.Length).Returns(5 * 1024 * 1024);
        mockFile.SetupGet(f => f.FileName).Returns("img.jpg");
        mockFile.SetupGet(f => f.ContentType).Returns("image/jpeg");

        var anexos = new List<IFormFile> { mockFile.Object };

        _solicitacaoRepository.Setup(r => r.Add(It.IsAny<Solicitacao>()));
        _mensagemRepository.Setup(r => r.Add(It.IsAny<Mensagem>()));

        _storage.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), ct))
                .ReturnsAsync("nome_do_arquivo_mockado.jpg");

        _solicitacaoAnexoRepository.Setup(s => s.Add(It.IsAny<SolicitacaoAnexo>()));

        _uow.Setup(u => u.CommitAsync(ct))
            .ThrowsAsync(new Exception("Database error"));

        _storage.Setup(s => s.DeleteByProtocoloAsync(It.IsAny<string>(), ct))
                .ThrowsAsync(new Exception("Delete failed"));

        // Act
        var result = await _sut.CriarSolicitacaoAsync(request, anexos, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Ocorreu um erro inesperado ao registrar a solicitação.");

        _solicitacaoRepository.Verify(r => r.Add(It.IsAny<Solicitacao>()), Times.Once);
        _mensagemRepository.Verify(r => r.Add(It.IsAny<Mensagem>()), Times.Once);
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), ct), Times.Once);
        _solicitacaoAnexoRepository.Verify(s => s.Add(It.IsAny<SolicitacaoAnexo>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);
        _storage.Verify(s => s.DeleteByProtocoloAsync(It.IsAny<string>(), ct), Times.Once);

        // logger é Loose, então não precisa verificar chamada
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_QuandoMultiplosAnexosSucesso_DeveRetornarSucesso()
    {
        // Arrange
        var request = _fixture.CreateValidRequest(anonimo: true);
        var ct = CancellationToken.None;

        var anexos = new List<IFormFile>();
        for (int i = 0; i < 3; i++)
        {
            var f = new Mock<IFormFile>();
            f.SetupGet(x => x.Length).Returns(2 * 1024 * 1024);
            f.SetupGet(x => x.FileName).Returns($"f{i}.jpg");
            f.SetupGet(x => x.ContentType).Returns("image/jpeg");
            anexos.Add(f.Object);
        }

        _solicitacaoRepository.Setup(r => r.Add(It.IsAny<Solicitacao>()));
        _mensagemRepository.Setup(r => r.Add(It.IsAny<Mensagem>()));

        _storage.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), ct))
                .ReturnsAsync("nome_do_arquivo_mockado.jpg");

        _solicitacaoAnexoRepository.Setup(s => s.Add(It.IsAny<SolicitacaoAnexo>()));

        _uow.Setup(u => u.CommitAsync(ct)).ReturnsAsync(true);

        _sendEmail.Setup(s => s.SendEmailSolicitacaoAsync(It.IsAny<string>(), null!))
                  .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CriarSolicitacaoAsync(request, anexos, ct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();

        _solicitacaoRepository.Verify(r => r.Add(It.IsAny<Solicitacao>()), Times.Once);
        _mensagemRepository.Verify(r => r.Add(It.IsAny<Mensagem>()), Times.Once);

        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), ct), Times.Exactly(3));
        _solicitacaoAnexoRepository.Verify(s => s.Add(It.IsAny<SolicitacaoAnexo>()), Times.Exactly(3));

        _uow.Verify(u => u.CommitAsync(ct), Times.Once);
        _sendEmail.Verify(s => s.SendEmailSolicitacaoAsync(It.IsAny<string>(), null!), Times.Once);
    }

    #endregion CriarSolicitacaoAsync Tests

    #region ListarSolicitacoesAsync Tests

    [Fact]
    public async Task ListarSolicitacoesAsync_QuandoDataFinalMenorQueInicial_DeveRetornarValidationError()
    {
        // Arrange
        var filtro = new SolicitacaoFilter
        {
            DataInicial = DateTime.Today,
            DataFinal = DateTime.Today.AddDays(-1)
        };

        // Act
        var result = await _sut.ListarSolicitacoesAsync(filtro, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.VALIDATION.ToString());
        result.Errors[0].Message.Should().Be("A data final deve ser maior ou igual à data inicial.");

        _solicitacaoRepository.VerifyNoOtherCalls();
        _mensagemRepository.VerifyNoOtherCalls();
        _uow.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ListarSolicitacoesAsync_QuandoRepositorioRetornaNull_DeveRetornarNotFound()
    {
        // Arrange
        var filtro = new SolicitacaoFilter
        {
            DataInicial = DateTime.Today.AddDays(-7),
            DataFinal = DateTime.Today
        };

        _solicitacaoRepository
            .Setup(r => r.ListarSolicitacoesAsync(filtro, It.IsAny<CancellationToken>()))!
            .ReturnsAsync((IEnumerable<Solicitacao>?)null);

        // Act
        var result = await _sut.ListarSolicitacoesAsync(filtro, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.NOT_FOUND.ToString());
        result.Errors[0].Message.Should().Be("Nenhuma solicitação encontrada com o status e período informados.");

        _solicitacaoRepository.Verify(r => r.ListarSolicitacoesAsync(filtro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarSolicitacoesAsync_QuandoRepositorioRetornaLista_DeveMapearResponse()
    {
        // Arrange
        var filtro = new SolicitacaoFilter
        {
            DataInicial = DateTime.Today.AddDays(-7),
            DataFinal = DateTime.Today
        };

        var s1 = _fixture.CreateSolicitacaoEntity(anonima: true, statusDescricao: "Em análise", id: 1);
        var s2 = _fixture.CreateSolicitacaoEntity(anonima: false, statusDescricao: "Concluída", id: 2);

        _solicitacaoRepository
            .Setup(r => r.ListarSolicitacoesAsync(filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Solicitacao> { s1, s2 });

        // Act
        var result = await _sut.ListarSolicitacoesAsync(filtro, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);

        var list = result.Value!.ToList();
        list[0].Id.Should().Be(1);
        list[0].Anonima.Should().BeTrue();
        list[0].Status.Should().Be("Em análise");

        list[1].Id.Should().Be(2);
        list[1].Anonima.Should().BeFalse();
        list[1].Status.Should().Be("Concluída");
        list[1].Nome.Should().NotBeNullOrWhiteSpace();

        _solicitacaoRepository.Verify(r => r.ListarSolicitacoesAsync(filtro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarSolicitacoesAsync_QuandoRepositorioLancaException_DeveRetornarInternalError()
    {
        // Arrange
        var filtro = new SolicitacaoFilter
        {
            DataInicial = DateTime.Today.AddDays(-7),
            DataFinal = DateTime.Today
        };

        _solicitacaoRepository
            .Setup(r => r.ListarSolicitacoesAsync(filtro, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));

        // Act
        var result = await _sut.ListarSolicitacoesAsync(filtro, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Ocorreu um erro inesperado ao buscar as solicitações.");
    }

    #endregion ListarSolicitacoesAsync Tests

    #region AtualizarStatus Tests

    [Fact]
    public async Task AtualizarStatus_QuandoSolicitacaoNaoEncontrada_DeveRetornarNotFound()
    {
        // Arrange
        var protocolo = "12345678901";
        var ct = CancellationToken.None;

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync((Solicitacao?)null);

        // Act
        var result = await _sut.AtualizarStatus(protocolo, novoStatusId: 2, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.NOT_FOUND.ToString());
        result.Errors[0].Message.Should().Be("Solicitação não encontrada.");

        _solicitacaoRepository.Verify(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct), Times.Once);
        _solicitacaoRepository.VerifyNoOtherCalls();
        _uow.VerifyNoOtherCalls();
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AtualizarStatus_QuandoEncontradaNaoAnonimaComEmail_DeveAtualizarCommitarEEnviarEmail()
    {
        // Arrange
        var protocolo = "12345678901";
        var ct = CancellationToken.None;

        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: false, id: 10);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        _solicitacaoRepository
            .Setup(r => r.AtualizarStatus(solicitacao, 2, ct))
            .ReturnsAsync(solicitacao);

        _uow
            .Setup(u => u.CommitAsync(ct))
            .ReturnsAsync(true);

        _sendEmail
            .Setup(s => s.SendEmailStatusChangeAsync(solicitacao))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.AtualizarStatus(protocolo, 2, ct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _solicitacaoRepository.Verify(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct), Times.Exactly(2));
        _solicitacaoRepository.Verify(r => r.AtualizarStatus(solicitacao, 2, ct), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);
        _sendEmail.Verify(s => s.SendEmailStatusChangeAsync(solicitacao), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatus_QuandoEncontradaAnonima_NaoDeveEnviarEmail()
    {
        // Arrange
        var protocolo = "12345678901";
        var ct = CancellationToken.None;

        // ✅ Solicitação anônima: Usuario é null, então não envia email
        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: true, id: 10);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        _solicitacaoRepository
            .Setup(r => r.AtualizarStatus(solicitacao, 2, ct))
            .ReturnsAsync(solicitacao);

        _uow
            .Setup(u => u.CommitAsync(ct))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.AtualizarStatus(protocolo, 2, ct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _solicitacaoRepository.Verify(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct), Times.Once);
        _solicitacaoRepository.Verify(r => r.AtualizarStatus(solicitacao, 2, ct), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);

        // ✅ Verificar que email NÃO foi enviado
        _sendEmail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AtualizarStatus_QuandoSendEmailLancaException_DeveRetornarInternalError()
    {
        // Arrange
        var protocolo = "12345678901";
        var ct = CancellationToken.None;

        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: false, id: 10);

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ReturnsAsync(solicitacao);

        _solicitacaoRepository
            .Setup(r => r.AtualizarStatus(solicitacao, 2, ct))
            .ReturnsAsync(solicitacao);

        _uow
            .Setup(u => u.CommitAsync(ct))
            .ReturnsAsync(true);

        _sendEmail
            .Setup(s => s.SendEmailStatusChangeAsync(solicitacao))
            .ThrowsAsync(new Exception("Email service down"));

        // Act
        var result = await _sut.AtualizarStatus(protocolo, 2, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Ocorreu um erro inesperado ao alterar o status das solicitações.");

        _solicitacaoRepository.Verify(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct), Times.Exactly(2));
        _solicitacaoRepository.Verify(r => r.AtualizarStatus(solicitacao, 2, ct), Times.Once);
        _uow.Verify(u => u.CommitAsync(ct), Times.Once);
        _sendEmail.Verify(s => s.SendEmailStatusChangeAsync(solicitacao), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatus_QuandoRepositorioLancaException_DeveRetornarInternalError()
    {
        // Arrange
        var protocolo = "12345678901";
        var ct = CancellationToken.None;

        _solicitacaoRepository
            .Setup(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct))
            .ThrowsAsync(new Exception("fail"));

        // Act
        var result = await _sut.AtualizarStatus(protocolo, 2, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Ocorreu um erro inesperado ao alterar o status das solicitações.");

        _solicitacaoRepository.Verify(r => r.ObterSolicitacaoPorProtocoloAsync(protocolo, ct), Times.Once);
        _sendEmail.VerifyNoOtherCalls();
    }

    #endregion AtualizarStatus Tests    

    #region DownloadAnexoAsync Tests

    [Fact]
    public async Task DownloadAnexoAsync_QuandoParametrosInvalidos_DeveRetornarValidationError()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Act
        var result = await _sut.DownloadAnexoAsync("", 0, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.VALIDATION.ToString());
        result.Errors[0].Message.Should().Be("Parâmetros inválidos.");

        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAnexoAsync_QuandoAnexoNaoExisteOuProtocoloNaoBate_DeveRetornarNotFound()
    {
        // Arrange
        var ct = CancellationToken.None;
        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: true);

        var protocolo = "AAA";
        var anexoId = 10;

        _solicitacaoAnexoRepository.Setup(s => s.ObterAnexoAsync(anexoId, ct))
                .ReturnsAsync(new SolicitacaoAnexo(
                    solicitacao: solicitacao,
                    protocolo: "OUTRO",
                    nomeArquivo: "x.bin",
                    nomeOriginal: "x.bin",
                    contentType: "application/octet-stream",
                    tamanhoOriginal: 1,
                    compactado: true
                ));

        // Act
        var result = await _sut.DownloadAnexoAsync(protocolo, anexoId, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.NOT_FOUND.ToString());
        result.Errors[0].Message.Should().Be("Anexo não encontrado.");

        _solicitacaoAnexoRepository.Verify(s => s.ObterAnexoAsync(anexoId, ct), Times.Once);
        _solicitacaoAnexoRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAnexoAsync_QuandoOpenReadLancaFileNotFound_DeveRetornarNotFound()
    {
        // Arrange
        var ct = CancellationToken.None;
        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: true);

        var protocolo = "AAA";
        var anexoId = 10;

        var anexo = new SolicitacaoAnexo(
            solicitacao: solicitacao,
            protocolo: protocolo,
            nomeArquivo: "x.bin",
            nomeOriginal: "orig.bin",
            contentType: "application/octet-stream",
            tamanhoOriginal: 1,
            compactado: true
        );

        _solicitacaoAnexoRepository.Setup(s => s.ObterAnexoAsync(anexoId, ct))
                .ReturnsAsync(anexo);

        _storage.Setup(s => s.OpenReadAsync(protocolo, anexo.NomeArquivo, anexo.Compactado, ct))
                .ThrowsAsync(new FileNotFoundException());

        // Act
        var result = await _sut.DownloadAnexoAsync(protocolo, anexoId, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.NOT_FOUND.ToString());
        result.Errors[0].Message.Should().Be("Arquivo não encontrado no storage.");
    }

    [Fact]
    public async Task DownloadAnexoAsync_QuandoOpenReadLancaIOException_DeveRetornarInternalError()
    {
        // Arrange
        var ct = CancellationToken.None;
        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: true);

        var protocolo = "AAA";
        var anexoId = 10;

        var anexo = new SolicitacaoAnexo(
            solicitacao: solicitacao,
            protocolo: protocolo,
            nomeArquivo: "x.bin",
            nomeOriginal: "orig.bin",
            contentType: "application/octet-stream",
            tamanhoOriginal: 1,
            compactado: true
        );

        _solicitacaoAnexoRepository.Setup(s => s.ObterAnexoAsync(anexoId, ct))
                .ReturnsAsync(anexo);

        _storage.Setup(s => s.OpenReadAsync(protocolo, anexo.NomeArquivo, anexo.Compactado, ct))
                .ThrowsAsync(new IOException("io"));

        // Act
        var result = await _sut.DownloadAnexoAsync(protocolo, anexoId, ct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorsEnum.INTERNAL_ERROR.ToString());
        result.Errors[0].Message.Should().Be("Erro ao ler o arquivo.");
    }

    [Fact]
    public async Task DownloadAnexoAsync_QuandoSucesso_DeveRetornarStreamContentTypeEDownloadName()
    {
        // Arrange
        var ct = CancellationToken.None;
        var solicitacao = _fixture.CreateSolicitacaoEntity(anonima: true);

        var protocolo = "AAA";
        var anexoId = 10;

        var anexo = new SolicitacaoAnexo(
            solicitacao: solicitacao,
            protocolo: protocolo,
            nomeArquivo: "x.bin",
            nomeOriginal: "orig.bin",
            contentType: "application/octet-stream",
            tamanhoOriginal: 1,
            compactado: true
        );

        _solicitacaoAnexoRepository.Setup(s => s.ObterAnexoAsync(anexoId, ct))
                .ReturnsAsync(anexo);

        var mem = new MemoryStream([1, 2, 3]);

        _storage.Setup(s => s.OpenReadAsync(protocolo, anexo.NomeArquivo, anexo.Compactado, ct))
                .ReturnsAsync(mem);

        // Act
        var result = await _sut.DownloadAnexoAsync(protocolo, anexoId, ct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Stream.Should().BeSameAs(mem);
        result.Value.ContentType.Should().Be("application/octet-stream");
        result.Value.DownloadName.Should().Be("orig.bin");
    }

    #endregion DownloadAnexoAsync Tests
}