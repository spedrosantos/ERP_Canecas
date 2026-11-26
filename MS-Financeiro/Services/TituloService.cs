using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.RegularExpressions;
using Financeiro;
using Microsoft.EntityFrameworkCore;
using MS_Financeiro.Data.Interfaces;
using MS_Financeiro.DTOs;
using MS_Financeiro.Repositories;
using MS_Financeiro.Repositories.Interfaces;
using MS_Financeiro.Services.Interfaces;

namespace MS_Financeiro.Services;

public sealed class TituloService(ITenantContextFactory factory, IHttpContextAccessor http) : ITituloService
{
    public async Task<TitulosPaginadosResponseDTO> ListarAsync(ListarTitulosDTO filtro, CancellationToken ct)
    {
        ValidarPeriodo(filtro);
        await using var contexto = await factory.CriarAsync(ct);
        await using var leitura = await contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var pagina = await new TituloRepository(contexto).ListarAsync(filtro, Hoje(), ct);
        await leitura.CommitAsync(ct);
        return pagina;
    }

    public async Task<TituloDetalheResponseDTO?> ObterAsync(long id, CancellationToken ct)
    {
        await using var contexto = await factory.CriarAsync(ct);
        await using var leitura = await contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var repositorio = new TituloRepository(contexto);
        var titulo = await repositorio.ObterAsync(id, Hoje(), ct);
        if (titulo is null)
            return null;

        var baixas = await repositorio.ListarBaixasAsync(id, ct);
        await leitura.CommitAsync(ct);
        return new(titulo, baixas.ConvertAll(Mapear));
    }

    public Task<BaixaResponseDTO?> RegistrarBaixaAsync(long id, RegistrarBaixaDTO dto, CancellationToken ct)
    {
        if (dto.Valor <= 0 || dto.Valor > 99999999999999999.99m || decimal.Round(dto.Valor, 2) != dto.Valor)
            throw new ValidationException("Informe um valor positivo com até duas casas decimais.");
        if (!Regex.IsMatch(dto.FormaPagamento, Baixa.PadraoFormaPagamento))
            throw new ValidationException("Forma de pagamento inválida.");

        return AlterarAsync(id, async (transacional, repositorio, titulo, usuarioId) =>
        {
            if (titulo.CanceladoEm is not null)
                throw new ConflitoFinanceiroException("O título está cancelado.");

            var baixado = await transacional.BaixasAtivas(id).SumAsync(x => x.Valor, ct);
            var saldo = titulo.Valor - baixado;
            if (saldo <= 0)
                throw new ConflitoFinanceiroException("O título está quitado.");
            if (dto.Valor > saldo)
                throw new ConflitoFinanceiroException("O valor da baixa excede o saldo do título.");

            var baixa = new Baixa
            {
                TituloId = id, Valor = dto.Valor, FormaPagamento = dto.FormaPagamento,
                UsuarioId = usuarioId, CriadoEm = DateTime.UtcNow,
                Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim()
            };
            repositorio.Adicionar(baixa);
            return baixa;
        }, ct);
    }

    public Task<BaixaResponseDTO?> EstornarBaixaAsync(long id, long baixaId, EstornarBaixaDTO dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Motivo) || dto.Motivo.Length > 500)
            throw new ValidationException("Informe o motivo do estorno com até 500 caracteres.");

        return AlterarAsync(id, async (_, repositorio, _, usuarioId) =>
        {
            var baixa = await repositorio.ObterBaixaAsync(id, baixaId, ct);
            if (baixa is null)
                return null;
            if (baixa.EstornadoEm is not null)
                throw new ConflitoFinanceiroException("A baixa já foi estornada.");

            baixa.EstornadoEm = DateTime.UtcNow;
            baixa.UsuarioEstornoId = usuarioId;
            baixa.MotivoEstorno = dto.Motivo.Trim();
            return baixa;
        }, ct);
    }

    public async Task<IndicadoresFinanceirosResponseDTO> ObterIndicadoresAsync(FiltroTitulosDTO filtro, CancellationToken ct)
    {
        ValidarPeriodo(filtro);
        await using var contexto = await factory.CriarAsync(ct);
        return await new TituloRepository(contexto).ObterIndicadoresAsync(filtro, Hoje(), ct);
    }

    private async Task<BaixaResponseDTO?> AlterarAsync(long id,
        Func<TitulosTransacionais, ITituloRepository, Titulo, long, Task<Baixa?>> alterar, CancellationToken ct)
    {
        var usuarioId = ObterUsuarioId();
        await using var contexto = await factory.CriarAsync(ct);
        await using var transacao = await contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var financeiro = new TitulosTransacionais(contexto);
        var titulo = await financeiro.BloquearPorIdAsync(id, ct);
        if (titulo is null)
            return null;

        var baixa = await alterar(financeiro, new TituloRepository(contexto), titulo, usuarioId);
        if (baixa is null)
            return null;

        await contexto.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
        return Mapear(baixa);
    }

    private long ObterUsuarioId()
    {
        var valor = http.HttpContext?.User.FindFirst("usuario_id")?.Value;
        return long.TryParse(valor, out var id) && id > 0
            ? id : throw new UnauthorizedAccessException("Usuário não identificado.");
    }

    private static void ValidarPeriodo(FiltroTitulosDTO filtro)
    {
        if (filtro.VencimentoInicio > filtro.VencimentoFim)
            throw new ValidationException("O vencimento inicial não pode ser posterior ao final.");
    }

    private static DateOnly Hoje() => TitulosTransacionais.DataLocal(DateTime.UtcNow);

    private static BaixaResponseDTO Mapear(Baixa baixa) =>
        new(baixa.Id, baixa.TituloId, baixa.Valor, baixa.FormaPagamento, baixa.UsuarioId,
            baixa.Observacao, baixa.CriadoEm, baixa.EstornadoEm, baixa.UsuarioEstornoId, baixa.MotivoEstorno);
}
