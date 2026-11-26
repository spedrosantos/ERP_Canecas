using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MS_Compra.Data;
using MS_Compra.Data.Interfaces;
using MS_Compra.DTOs.Pedidos;
using MS_Compra.Entities;
using MS_Compra.Repositories;
using MS_Compra.Repositories.Interfaces;
using MS_Compra.Services.Interfaces;

namespace MS_Compra.Services;

public class PedidoService(ITenantContextFactory contextFactory, IHttpContextAccessor httpContextAccessor)
    : IPedidoService
{
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
        var pedido = await new PedidoRepository(context).ObterAsync(id, ct);
        return pedido is null ? null : Mapear(pedido);
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
        await transacao.CommitAsync(ct);
        return Mapear(pedido);
    }

    public Task<PedidoDetalheResponseDTO?> EditarAsync(long id, SalvarPedidoDTO dto, CancellationToken ct) =>
        AlterarPendenteAsync(id, (_, repository, pedido) => PreencherAsync(repository, pedido, dto, ct), ct);

    public Task<PedidoDetalheResponseDTO?> CancelarAsync(long id, CancellationToken ct) =>
        AlterarPendenteAsync(id, (_, _, pedido) =>
        {
            pedido.Situacao = Pedido.Cancelado;
            return Task.CompletedTask;
        }, ct);

    public async Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarFornecedoresAsync(
        ListarCadastrosDTO filtro, CancellationToken ct)
    {
        await using var context = await contextFactory.CriarAsync(ct);
        return await new PedidoRepository(context).ListarFornecedoresAsync(filtro, ct);
    }

    public async Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarProdutosAsync(
        ListarCadastrosDTO filtro, CancellationToken ct)
    {
        await using var context = await contextFactory.CriarAsync(ct);
        return await new PedidoRepository(context).ListarProdutosAsync(filtro, ct);
    }

    private async Task<PedidoDetalheResponseDTO?> AlterarPendenteAsync(
        long id, Func<TenantDbContext, IPedidoRepository, Pedido, Task> alterar, CancellationToken ct)
    {
        await using var context = await contextFactory.CriarAsync(ct);
        await using var transacao = await context.Database.BeginTransactionAsync(ct);

        var repository = new PedidoRepository(context);
        var pedido = await repository.BloquearAsync(id, ct);
        if (pedido is null)
            return null;
        if (pedido.Situacao != Pedido.Pendente)
            throw new PedidoSituacaoException();

        await alterar(context, repository, pedido);
        pedido.AtualizadoEm = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
        return Mapear(pedido);
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
            if (item.Quantidade <= 0 || item.Quantidade > 999999999999.999m ||
                decimal.Round(item.Quantidade, 3) != item.Quantidade)
                throw new ValidationException("Quantidade inválida. Informe até três casas decimais.");
            if (item.CustoUnitario is not decimal custo || custo < 0 || custo > 99999999999.9999m ||
                decimal.Round(custo, 4) != custo)
                throw new ValidationException("Custo unitário inválido. Informe até quatro casas decimais.");

            total += item.Quantidade * custo;
            if (total >= limiteTotal + 0.005m)
                throw new ValidationException("O total excede o limite do pedido.");
        }

        await ValidarCadastrosAsync(repository, dto.FornecedorId, produtos.ToArray(), ct);

        pedido.FornecedorId = dto.FornecedorId;
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
            item.CustoUnitario = entrada.CustoUnitario!.Value;
        }
    }

    private static async Task ValidarCadastrosAsync(
        IPedidoRepository repository, long fornecedorId, long[] produtos, CancellationToken ct)
    {
        if (fornecedorId <= 0 || !await repository.BloquearFornecedorAtivoAsync(fornecedorId, ct))
            throw new ValidationException("Fornecedor inexistente ou inativo.");

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

    private static PedidoDetalheResponseDTO Mapear(Pedido pedido) => new(
        new PedidoResponseDTO(pedido.Id, pedido.FornecedorId, pedido.UsuarioId, pedido.Situacao,
            pedido.ValorTotal, pedido.Observacao, pedido.CriadoEm, pedido.AtualizadoEm, pedido.RecebidoEm),
        pedido.Itens.OrderBy(x => x.Id).Select(x =>
            new PedidoItemResponseDTO(x.Id, x.ProdutoId, x.Quantidade, x.CustoUnitario)).ToArray());
}
