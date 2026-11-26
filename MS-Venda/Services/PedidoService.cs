using Financeiro;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MS_Venda.Data;
using MS_Venda.Data.Interfaces;
using MS_Venda.DTOs.Pedidos;
using MS_Venda.Entities;
using MS_Venda.Repositories;
using MS_Venda.Repositories.Interfaces;
using MS_Venda.Services.Interfaces;
using MS_Estoque.Entities;
using MS_Estoque.Repositories;
using MS_Estoque.Rules;

namespace MS_Venda.Services;

public class PedidoService(ITenantContextFactory contextFactory, IHttpContextAccessor httpContextAccessor)
    : IPedidoService
{
    public async Task<DashboardVendaDTO> ObterDashboardAsync(DateOnly inicio, DateOnly fim, CancellationToken ct)
    {
        if (inicio > fim)
            throw new ValidationException("A data inicial não pode ser maior que a final.");

        await using var context = await contextFactory.CriarAsync(ct);
        return await new PedidoRepository(context).ObterDashboardAsync(inicio, fim, ct);
    }

    public async Task<PaginadoResponseDTO<PedidoResponseDTO>> ListarAsync(ListarPedidosDTO filtro, CancellationToken ct)
    {
        if (filtro.DataInicio > filtro.DataFim)
            throw new ValidationException("A data inicial não pode ser maior que a final.");

        await using var context = await contextFactory.CriarAsync(ct);
        return await new PedidoRepository(context).ListarAsync(filtro, ct);
    }

    public async Task<PedidoDetalheResponseDTO?> ObterAsync(long id, CancellationToken ct)
    {
        await using var context = await contextFactory.CriarAsync(ct);
        var repository = new PedidoRepository(context);
        var pedido = await repository.ObterAsync(id, ct);
        return pedido is null ? null : await MapearAsync(repository, pedido, ct);
    }

    public async Task<PedidoDetalheResponseDTO> CadastrarAsync(SalvarPedidoDTO dto, CancellationToken ct)
    {
        await using var context = await contextFactory.CriarAsync(ct);
        await using var transacao = await context.Database.BeginTransactionAsync(ct);

        var repository = new PedidoRepository(context);
        var pedido = new Pedido { UsuarioId = ObterUsuarioId(), CriadoEm = DateTime.UtcNow };
        await PreencherAsync(repository, pedido, dto, ct);
        repository.Adicionar(pedido);

        await context.SaveChangesAsync(ct);
        var resposta = await MapearAsync(repository, pedido, ct);
        await transacao.CommitAsync(ct);
        return resposta;
    }

    public Task<PedidoDetalheResponseDTO?> EditarAsync(long id, SalvarPedidoDTO dto, CancellationToken ct) =>
        AlterarAsync(id, (_, repository, pedido) =>
        {
            ExigirPendente(pedido, "Somente vendas pendentes podem ser editadas.");
            return PreencherAsync(repository, pedido, dto, ct);
        }, ct);

    public Task<PedidoDetalheResponseDTO?> ConfirmarAsync(long id, ConfirmarPedidoDTO? dto, CancellationToken ct)
    {
        var usuarioId = ObterUsuarioId();
        return AlterarAsync(id, async (context, repository, pedido) =>
        {
            ExigirPendente(pedido, "Somente vendas pendentes podem ser confirmadas.");
            if (pedido.Itens.Count == 0)
                throw new ValidationException("A venda não contém itens.");

            var itens = pedido.Itens.OrderBy(x => x.ProdutoId).ToArray();
            var produtos = itens.Select(x => x.ProdutoId).ToArray();
            if (produtos.Distinct().Count() != produtos.Length)
                throw new ValidationException("A venda contém produtos duplicados.");

            await ValidarCadastrosAsync(repository, pedido.ClienteId, produtos, ct);

            var movimentacoes = new MovimentacaoRepository(context);
            var confirmadoEm = DateTime.UtcNow;

            // O pedido é bloqueado primeiro. Os saldos são sempre bloqueados por produto crescente.
            foreach (var item in itens)
            {
                var saldo = await movimentacoes.BloquearSaldoAsync(item.ProdutoId, ct);
                if (saldo.Quantidade < item.Quantidade)
                    throw new ValidationException($"Saldo insuficiente para o produto {item.ProdutoId}.");

                var custo = CalculoEstoque.RegistrarSaida(saldo, item.Quantidade);
                item.CustoUnitario = custo;
                movimentacoes.Adicionar(new MovimentacaoEstoque
                {
                    ProdutoId = item.ProdutoId,
                    Tipo = "SAIDA",
                    Quantidade = item.Quantidade,
                    CustoUnitario = custo,
                    Origem = "VENDA",
                    ReferenciaId = pedido.Id,
                    UsuarioId = usuarioId,
                    CriadoEm = confirmadoEm
                });
            }

            pedido.Situacao = Pedido.Confirmado;
            pedido.ConfirmadoEm = confirmadoEm;
            new TitulosTransacionais(context).GerarParaVenda(
                pedido.Id, pedido.ValorTotal, dto?.Vencimento, usuarioId, confirmadoEm);
        }, ct);
    }

    public Task<PedidoDetalheResponseDTO?> CancelarAsync(long id, CancellationToken ct)
    {
        var usuarioId = ObterUsuarioId();
        return AlterarAsync(id, async (context, repository, pedido) =>
        {
            var canceladoEm = DateTime.UtcNow;
            switch (pedido.Situacao)
            {
                case Pedido.Pendente:
                    break;
                case Pedido.Confirmado:
                    await new TitulosTransacionais(context).CancelarDaVendaAsync(pedido.Id, canceladoEm, ct);
                    await EstornarSaidasAsync(context, repository, pedido, usuarioId, canceladoEm, ct);
                    break;
                default:
                    throw new PedidoSituacaoException("Somente vendas pendentes ou confirmadas podem ser canceladas.");
            }

            pedido.Situacao = Pedido.Cancelado;
            pedido.CanceladoEm = canceladoEm;
        }, ct);
    }

    private static async Task EstornarSaidasAsync(
        TenantDbContext context, IPedidoRepository repository, Pedido pedido,
        long usuarioId, DateTime canceladoEm, CancellationToken ct)
    {
        var saidas = await repository.ListarSaidasDaVendaAsync(pedido.Id, ct);
        var itens = pedido.Itens.OrderBy(x => x.ProdutoId).ToArray();
        if (saidas.Count == 0 || saidas.Count != itens.Length ||
            saidas.Select(x => x.ProdutoId).Distinct().Count() != saidas.Count ||
            saidas.Zip(itens).Any(par => par.First.ProdutoId != par.Second.ProdutoId ||
                par.First.Quantidade != par.Second.Quantidade || par.First.CustoUnitario != par.Second.CustoUnitario))
        {
            throw new PedidoSituacaoException("As saídas de estoque da venda estão inconsistentes.");
        }

        var movimentacoes = new MovimentacaoRepository(context);
        foreach (var saida in saidas)
        {
            var saldo = await movimentacoes.BloquearSaldoAsync(saida.ProdutoId, ct);
            if (await movimentacoes.PossuiEstornoAsync(saida.Id, ct))
                throw new PedidoSituacaoException("Uma saída desta venda já foi estornada.");

            var custo = CalculoEstoque.RegistrarEstorno(saldo, saida);
            movimentacoes.Adicionar(new MovimentacaoEstoque
            {
                ProdutoId = saida.ProdutoId,
                Tipo = "ENTRADA",
                Quantidade = saida.Quantidade,
                CustoUnitario = custo,
                Origem = "ESTORNO",
                MovimentacaoOriginalId = saida.Id,
                UsuarioId = usuarioId,
                Justificativa = $"Cancelamento da venda {pedido.Id}.",
                CriadoEm = canceladoEm
            });
        }
    }

    public async Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarClientesAsync(
        ListarCadastrosDTO filtro, CancellationToken ct)
    {
        await using var context = await contextFactory.CriarAsync(ct);
        return await new PedidoRepository(context).ListarClientesAsync(filtro, ct);
    }

    public async Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarProdutosAsync(
        ListarCadastrosDTO filtro, CancellationToken ct)
    {
        await using var context = await contextFactory.CriarAsync(ct);
        return await new PedidoRepository(context).ListarProdutosAsync(filtro, ct);
    }

    public async Task<DisponibilidadeProdutoResponseDTO?> ObterDisponibilidadeAsync(long produtoId, CancellationToken ct)
    {
        await using var context = await contextFactory.CriarAsync(ct);
        return await new PedidoRepository(context).ObterDisponibilidadeAsync(produtoId, ct);
    }

    private static void ExigirPendente(Pedido pedido, string mensagem)
    {
        if (pedido.Situacao != Pedido.Pendente)
            throw new PedidoSituacaoException(mensagem);
    }

    private async Task<PedidoDetalheResponseDTO?> AlterarAsync(
        long id, Func<TenantDbContext, IPedidoRepository, Pedido, Task> alterar, CancellationToken ct)
    {
        await using var context = await contextFactory.CriarAsync(ct);
        await using var transacao = await context.Database.BeginTransactionAsync(ct);

        var repository = new PedidoRepository(context);
        var pedido = await repository.BloquearAsync(id, ct);
        if (pedido is null)
            return null;

        await alterar(context, repository, pedido);
        pedido.AtualizadoEm = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        var resposta = await MapearAsync(repository, pedido, ct);
        await transacao.CommitAsync(ct);
        return resposta;
    }

    private static async Task PreencherAsync(
        IPedidoRepository repository, Pedido pedido, SalvarPedidoDTO dto, CancellationToken ct)
    {
        if (dto.Itens is not { Count: > 0 })
            throw new ValidationException("Informe ao menos um item.");

        var produtos = new HashSet<long>();
        const decimal limiteTotal = 99999999999999999.99m;
        decimal total = 0;

        foreach (var item in dto.Itens)
        {
            if (item is null || item.ProdutoId <= 0)
                throw new ValidationException("Informe um produto válido para cada item.");
            if (!produtos.Add(item.ProdutoId))
                throw new ValidationException("Um produto só pode aparecer uma vez por pedido.");
            CalculoEstoque.ValidarQuantidade(item.Quantidade);
            var preco = item.PrecoUnitario ?? throw new ValidationException("Informe o preço unitário.");
            if (preco <= 0 || preco > 99999999999.9999m || decimal.Round(preco, 4) != preco)
                throw new ValidationException("Informe um preço unitário positivo com até quatro casas decimais.");

            total += item.Quantidade * preco;
            if (total >= limiteTotal + 0.005m)
                throw new ValidationException("O total excede o limite do pedido.");
        }

        await ValidarCadastrosAsync(repository, dto.ClienteId, produtos.ToArray(), ct);

        pedido.ClienteId = dto.ClienteId;
        pedido.Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim();
        pedido.ValorTotal = decimal.Round(total, 2, MidpointRounding.AwayFromZero);

        var existentes = pedido.Itens.ToDictionary(x => x.ProdutoId);
        foreach (var item in existentes.Values.Where(x => !produtos.Contains(x.ProdutoId)))
        {
            pedido.Itens.Remove(item);
            repository.RemoverItem(item);
        }

        foreach (var entrada in dto.Itens)
        {
            if (!existentes.TryGetValue(entrada.ProdutoId, out var item))
            {
                item = new PedidoItem { ProdutoId = entrada.ProdutoId };
                pedido.Itens.Add(item);
            }

            item.Quantidade = entrada.Quantidade;
            item.PrecoUnitario = entrada.PrecoUnitario!.Value;
        }
    }

    private static async Task ValidarCadastrosAsync(
        IPedidoRepository repository, long clienteId, long[] produtos, CancellationToken ct)
    {
        if (clienteId <= 0 || !await repository.BloquearClienteAtivoAsync(clienteId, ct))
            throw new ValidationException("Cliente inexistente ou inativo.");

        if (await repository.BloquearProdutosAtivosAsync(produtos, ct) != produtos.Length)
            throw new ValidationException("O pedido contém produto inexistente ou inativo.");
    }

    private long ObterUsuarioId()
    {
        var valor = httpContextAccessor.HttpContext?.User.FindFirst("usuario_id")?.Value;
        if (!long.TryParse(valor, out var id) || id <= 0)
            throw new UnauthorizedAccessException("Usuário não identificado.");

        return id;
    }

    private static async Task<PedidoDetalheResponseDTO> MapearAsync(
        IPedidoRepository repository, Pedido pedido, CancellationToken ct)
    {
        var produtos = await repository.ObterProdutosAsync(
            pedido.Itens.Select(x => x.ProdutoId).Distinct().ToArray(), ct);
        var itens = pedido.Itens.OrderBy(x => x.Id).Select(item =>
        {
            produtos.TryGetValue(item.ProdutoId, out var produto);
            return new PedidoItemResponseDTO(item.Id, item.ProdutoId, item.Quantidade,
                item.PrecoUnitario, item.CustoUnitario, produto?.Nome, produto?.UnidadeMedida);
        }).ToArray();

        var nomeCliente = await repository.ObterNomeClienteAsync(pedido.ClienteId, ct);
        return new(new PedidoResponseDTO(pedido.Id, pedido.ClienteId, pedido.UsuarioId, pedido.Situacao,
            pedido.ValorTotal, pedido.Observacao, pedido.CriadoEm, pedido.AtualizadoEm, pedido.ConfirmadoEm, pedido.CanceladoEm),
            itens, nomeCliente);
    }
}
