using Ardalis.Specification;
using CanalDenuncias.Domain.Entities;
using CanalDenuncias.Domain.Filters;

namespace CanalDenuncias.Infra.Data.Repositories.Filters;

public class SolicitacaoFilterSpecification : Specification<Solicitacao>
{
    public SolicitacaoFilterSpecification(SolicitacaoFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Protocolo))
            Query.Where(x => x.Protocolo == filter.Protocolo);

        if (filter.StatusId.HasValue)
            Query.Where(x => x.StatusSolicitacaoId == filter.StatusId.Value);

        // Garante que a data inicial comece no primeiro segundo do dia: DataInicial.Value.Date
        if (filter.DataInicial.HasValue)
            Query.Where(x => x.DataOcorrencia >= filter.DataInicial.Value.Date);

        // Garante que a data final vá até o último segundo do dia: DataFinal.Value.AddDays(1).AddTicks(-1)
        if (filter.DataFinal.HasValue)
            Query.Where(x => x.DataOcorrencia <= filter.DataFinal.Value.AddDays(1).AddTicks(-1));

        // Paginação e Ordenação

        if (filter.Page <= 0)
            Query.OrderByDescending(x => x.DataOcorrencia);
        else
            Query.OrderByDescending(x => x.DataOcorrencia)
                 .Skip((filter.Page - 1) * filter.PageSize)
                 .Take(filter.PageSize);
    }
}