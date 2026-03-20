namespace CanalDenuncias.Domain.Filters;

public record SolicitacaoFilter(
    string? Protocolo = null,
    int? StatusId = null,
    DateTime? DataInicial = null,
    DateTime? DataFinal = null,
    int Page = 0,
    int PageSize = 10);