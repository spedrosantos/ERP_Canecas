using System.Data;
using Microsoft.EntityFrameworkCore;
using MS_Identidade.Data;
using MS_Identidade.Data.Interfaces;
using MS_Identidade.DTOs.Usuarios;
using MS_Identidade.Repositories;
using MS_Identidade.Repositories.Interfaces;
using MS_Identidade.Services.Interfaces;
using Npgsql;

namespace MS_Identidade.Services;

public class PermissaoService : IPermissaoService
{
    private const string GerenciarPermissoes = "USUARIO_PERMISSOES";

    private readonly IEmpresaRepository _empresaRepository;
    private readonly ITenantContextFactory _contextFactory;

    public PermissaoService(
        IEmpresaRepository empresaRepository,
        ITenantContextFactory contextFactory)
    {
        _empresaRepository = empresaRepository;
        _contextFactory = contextFactory;
    }

    public async Task<bool> PossuiPermissaoAsync(
        long empresaId,
        long usuarioId,
        string codigoFeature)
    {
        await using var context = await CriarContextoAsync(empresaId);
        if (context is null)
            return false;

        var usuarioRepository = new UsuarioRepository(context);

        var usuario = await usuarioRepository
            .ObterPorIdAsync(usuarioId);

        if (usuario == null || !usuario.Ativo)
            return false;

        var permissaoRepository = new PermissaoRepository(context);

        var features = await permissaoRepository
            .ListarPorUsuarioAsync(usuarioId);

        return PossuiPermissaoEfetiva(features, codigoFeature);
    }

    public async Task<List<FeatureResponseDTO>> ListarFeaturesAsync(long empresaId)
    {
        await using var context = await CriarContextoAsync(empresaId)
            ?? throw new InvalidOperationException("Empresa inexistente ou inativa.");
        var features = await new PermissaoRepository(context).ListarFeaturesAtivasAsync();

        return features.ConvertAll(MapearFeature);
    }

    public async Task<List<FeatureResponseDTO>?> ListarEfetivasAsync(long empresaId, long usuarioId)
    {
        await using var context = await CriarContextoAsync(empresaId);
        if (context is null)
            return null;

        var situacoes = await new UsuarioRepository(context).ListarSituacoesAsync([usuarioId]);
        if (!situacoes.GetValueOrDefault(usuarioId))
            return null;

        var atribuidas = await new PermissaoRepository(context).ListarPorUsuarioAsync(usuarioId);
        var efetivas = ObterIdsEfetivos(atribuidas);

        return atribuidas.Where(feature => efetivas.Contains(feature.Id))
            .OrderBy(feature => feature.Codigo, StringComparer.Ordinal)
            .Select(MapearFeature).ToList();
    }

    public async Task<List<FeatureResponseDTO>?> ListarAtribuidasAsync(long empresaId, long usuarioId)
    {
        await using var context = await CriarContextoAsync(empresaId)
            ?? throw new InvalidOperationException("Empresa inexistente ou inativa.");

        var situacoes = await new UsuarioRepository(context).ListarSituacoesAsync([usuarioId]);
        if (!situacoes.GetValueOrDefault(usuarioId))
            return null;

        var features = await new PermissaoRepository(context).ListarAtribuidasAsync(usuarioId);

        return features.ConvertAll(MapearFeature);
    }

    public async Task<PermissoesEmLoteResponseDTO?> ConsultarLoteAsync(long empresaId, long operadorId)
    {
        await using var context = await CriarContextoAsync(empresaId);
        if (context is null)
            return null;

        var usuarios = await new UsuarioRepository(context).ListarAtivosAsync();
        if (!usuarios.Any(usuario => usuario.Id == operadorId))
            return null;

        var repository = new PermissaoRepository(context);
        var atribuidas = await repository.ListarPorUsuarioAsync(operadorId);
        if (!PossuiPermissaoEfetiva(atribuidas, GerenciarPermissoes))
            return null;

        var features = await repository.ListarFeaturesAtivasAsync();
        var porUsuario = await repository.ListarAtribuidasAsync(usuarios.Select(u => u.Id).ToArray());
        return new(
            usuarios.Select(u => new UsuarioPermissoesResponseDTO(u.Id, u.Nome, u.Ativo, u.Id == operadorId,
                porUsuario[u.Id].Where(f => f.Ativo).Select(f => f.Id).ToArray())).ToList(),
            features.ConvertAll(MapearFeature),
            ObterIdsEfetivos(features));
    }

    public async Task<ResultadoAtualizacaoPermissoes> AtualizarEmLoteAsync(
        long empresaId, long operadorId, AtualizarPermissoesEmLoteDTO dto)
    {
        var usuariosIds = dto.UsuarioIds.ToHashSet();
        if (!SelecaoValida(usuariosIds, dto.UsuarioIds.Count))
            return ResultadoAtualizacaoPermissoes.UsuariosInvalidos;

        var featureIds = dto.ConcederIds.Concat(dto.RevogarIds).ToHashSet();
        if (!SelecaoValida(featureIds, dto.ConcederIds.Count + dto.RevogarIds.Count))
            return ResultadoAtualizacaoPermissoes.FeaturesInvalidas;

        await using var context = await CriarContextoAsync(empresaId);
        if (context is null)
            return ResultadoAtualizacaoPermissoes.SemAutoridade;

        var repository = new PermissaoRepository(context);
        try
        {
            // A alteração de situação e a atualização individual usam o mesmo isolamento.
            await using var transacao = await context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var porUsuarioAtivo = await repository.ListarPorUsuariosAtivosAsync();
            if (!PossuiPermissaoEfetiva(porUsuarioAtivo[operadorId], GerenciarPermissoes))
                return ResultadoAtualizacaoPermissoes.SemAutoridade;

            var situacoes = await new UsuarioRepository(context).ListarSituacoesAsync(usuariosIds);
            if (situacoes.Count != usuariosIds.Count || situacoes.Values.Any(ativo => !ativo))
                return ResultadoAtualizacaoPermissoes.UsuarioInexistente;

            var catalogo = (await repository.ListarCatalogoAsync()).ToDictionary(f => f.Id);
            var ativas = catalogo.Values.Where(f => f.Ativo).ToDictionary(f => f.Id);
            if (featureIds.Any(id => !ativas.ContainsKey(id)))
                return ResultadoAtualizacaoPermissoes.FeaturesInvalidas;

            if (featureIds.Any(id => !PossuiHierarquiaCompleta(ativas[id], ativas)))
                return ResultadoAtualizacaoPermissoes.HierarquiaInvalida;

            var concedidas = IncluirAncestrais(dto.ConcederIds.ToHashSet(), ativas);
            var revogarIds = dto.RevogarIds.ToHashSet();
            var revogadas = ativas.Values.Where(f => PossuiAncestralSelecionado(f, revogarIds, catalogo))
                .Select(f => f.Id).ToHashSet();
            if (concedidas.Overlaps(revogadas))
                return ResultadoAtualizacaoPermissoes.HierarquiaInvalida;

            var atribuidas = await repository.ListarAtribuidasAsync(usuariosIds);
            var gerenciadores = porUsuarioAtivo
                .Where(grupo => PossuiPermissaoEfetiva(grupo, GerenciarPermissoes))
                .Select(grupo => grupo.Key).ToHashSet();
            var incluir = new List<UsuarioFeature>();
            var remover = new List<UsuarioFeature>();

            foreach (var usuarioId in usuariosIds)
            {
                var antes = atribuidas[usuarioId].Where(f => f.Ativo).ToDictionary(f => f.Id);
                var novas = concedidas.Except(antes.Keys).ToArray();
                var removidas = revogadas.Intersect(antes.Keys).ToArray();
                var depois = antes.Keys.Except(removidas).Union(novas)
                    .ToDictionary(id => id, id => ativas[id]);
                if (situacoes[usuarioId] && PossuiPermissaoEfetiva(depois.Values, GerenciarPermissoes))
                    gerenciadores.Add(usuarioId);
                else
                    gerenciadores.Remove(usuarioId);

                incluir.AddRange(novas.Select(featureId => new UsuarioFeature
                {
                    UsuarioId = usuarioId,
                    FeatureId = featureId
                }));
                remover.AddRange(removidas.Select(featureId => new UsuarioFeature
                {
                    UsuarioId = usuarioId,
                    FeatureId = featureId
                }));
            }

            if (gerenciadores.Count == 0)
                return ResultadoAtualizacaoPermissoes.UltimoGerenciador;

            await repository.AtualizarVinculosAsync(incluir, remover);
            await transacao.CommitAsync();

            return ResultadoAtualizacaoPermissoes.Atualizado;
        }
        catch (Exception ex) when (ConflitoDeGravacao(ex))
        {
            return ResultadoAtualizacaoPermissoes.AlteracaoSimultanea;
        }
    }

    public async Task<ResultadoAtualizacaoPermissoes> AtualizarAsync(
        long empresaId, long operadorId, long usuarioId, IReadOnlyCollection<long> featureIds)
    {
        var solicitadas = featureIds.ToHashSet();
        if (solicitadas.Count != featureIds.Count || solicitadas.Any(id => id <= 0))
            return ResultadoAtualizacaoPermissoes.FeaturesInvalidas;

        await using var context = await CriarContextoAsync(empresaId)
            ?? throw new InvalidOperationException("Empresa inexistente ou inativa.");
        var repository = new PermissaoRepository(context);

        try
        {
            // Usa o mesmo isolamento da mudança de situação, incluindo as leituras da autorização.
            await using var transacao = await context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var porUsuarioAtivo = await repository.ListarPorUsuariosAtivosAsync();
            if (!PossuiPermissaoEfetiva(porUsuarioAtivo[operadorId], GerenciarPermissoes))
                return ResultadoAtualizacaoPermissoes.SemAutoridade;

            var usuario = await new UsuarioRepository(context).ObterPorIdAsync(usuarioId);
            if (usuario is not { Ativo: true })
                return ResultadoAtualizacaoPermissoes.UsuarioInexistente;

            var catalogo = (await repository.ListarFeaturesAtivasAsync()).ToDictionary(f => f.Id);
            if (solicitadas.Any(id => !catalogo.ContainsKey(id)))
                return ResultadoAtualizacaoPermissoes.FeaturesInvalidas;

            var atribuidas = await repository.ListarAtribuidasAsync(usuarioId);
            var atuais = atribuidas.Select(f => f.Id).ToHashSet();
            var incluir = solicitadas.Except(atuais).ToArray();
            var remover = atribuidas.Where(f => f.Ativo).Select(f => f.Id)
                .Except(solicitadas).ToArray();
            var ativasDepois = atuais.Except(remover).Concat(incluir)
                .Where(catalogo.ContainsKey)
                .ToDictionary(id => id, id => catalogo[id]);

            if (solicitadas.Any(id => !PossuiHierarquiaCompleta(catalogo[id], ativasDepois)))
                return ResultadoAtualizacaoPermissoes.HierarquiaInvalida;

            if (!(usuario.Ativo && PossuiPermissaoEfetiva(ativasDepois.Values, GerenciarPermissoes))
                && !porUsuarioAtivo.Any(outro => outro.Key != usuarioId
                    && PossuiPermissaoEfetiva(outro, GerenciarPermissoes)))
                return ResultadoAtualizacaoPermissoes.UltimoGerenciador;

            await repository.AtualizarVinculosAsync(usuarioId, incluir, remover);
            await transacao.CommitAsync();

            return ResultadoAtualizacaoPermissoes.Atualizado;
        }
        catch (Exception ex) when (ConflitoDeGravacao(ex))
        {
            return ResultadoAtualizacaoPermissoes.AlteracaoSimultanea;
        }
    }

    private static bool SelecaoValida(HashSet<long> ids, int quantidade)
    {
        return quantidade > 0 && ids.Count == quantidade && ids.All(id => id > 0);
    }

    private static HashSet<long> ObterIdsEfetivos(IEnumerable<Feature> features)
    {
        var atribuidas = features.ToDictionary(f => f.Id);
        return atribuidas.Values.Where(f => PossuiHierarquiaCompleta(f, atribuidas))
            .Select(f => f.Id).ToHashSet();
    }

    private static HashSet<long> IncluirAncestrais(
        HashSet<long> selecionadas, IReadOnlyDictionary<long, Feature> catalogo)
    {
        var resultado = new HashSet<long>(selecionadas);
        foreach (var id in selecionadas)
        {
            var atual = catalogo[id];
            while (atual.IdFeaturePai is long paiId && resultado.Add(paiId))
                atual = catalogo[paiId];
        }

        return resultado;
    }

    private static bool PossuiAncestralSelecionado(
        Feature feature, HashSet<long> selecionadas, IReadOnlyDictionary<long, Feature> catalogo)
    {
        var visitadas = new HashSet<long>();
        while (visitadas.Add(feature.Id))
        {
            if (selecionadas.Contains(feature.Id))
                return true;

            if (feature.IdFeaturePai is not long paiId || !catalogo.TryGetValue(paiId, out var pai))
                return false;

            feature = pai;
        }

        return false;
    }

    private static bool ConflitoDeGravacao(Exception ex)
    {
        return ex.GetBaseException() is PostgresException
            {
                SqlState: PostgresErrorCodes.SerializationFailure
                    or PostgresErrorCodes.DeadlockDetected
                    or PostgresErrorCodes.UniqueViolation
            }
            || ex is DbUpdateConcurrencyException;
    }

    private async Task<TenantDbContext?> CriarContextoAsync(long empresaId)
    {
        var empresa = await _empresaRepository.ObterPorIdAsync(empresaId);

        return empresa is { Ativo: true } ? _contextFactory.Criar(empresa.NomeBanco) : null;
    }

    private static FeatureResponseDTO MapearFeature(Feature feature)
    {
        return new(feature.Id, feature.IdFeaturePai, feature.Codigo, feature.Nome, feature.Ativo);
    }

    internal static bool PossuiPermissaoEfetiva(
        IEnumerable<Feature> features,
        string codigoFeature)
    {
        var feature = features.FirstOrDefault(
            x => x.Codigo == codigoFeature);

        return feature is not null && PossuiHierarquiaCompleta(
            feature, features.ToDictionary(f => f.Id));
    }

    private static bool PossuiHierarquiaCompleta(
        Feature feature, IReadOnlyDictionary<long, Feature> atribuidas)
    {
        var visitadas = new HashSet<long>();
        while (visitadas.Add(feature.Id))
        {
            if (feature.IdFeaturePai is not long paiId)
                return true;

            if (!atribuidas.TryGetValue(paiId, out var pai))
                return false;

            feature = pai;
        }

        return false;
    }
}
