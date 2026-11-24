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

    public async Task<List<FeatureResponseDTO>?> ListarAtribuidasAsync(long empresaId, long usuarioId)
    {
        await using var context = await CriarContextoAsync(empresaId)
            ?? throw new InvalidOperationException("Empresa inexistente ou inativa.");

        if (await new UsuarioRepository(context).ObterPorIdAsync(usuarioId) is null)
            return null;

        var features = await new PermissaoRepository(context).ListarAtribuidasAsync(usuarioId);

        return features.ConvertAll(MapearFeature);
    }

    public async Task<ResultadoAtualizacaoPermissoes> AtualizarAsync(
        long empresaId, long operadorId, long usuarioId, IReadOnlyCollection<long> featureIds)
    {
        if (operadorId == usuarioId)
            return ResultadoAtualizacaoPermissoes.AlteracaoPropria;

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
            var featuresOperador = porUsuarioAtivo[operadorId].ToDictionary(f => f.Id);
            var autoridade = featuresOperador.Values
                .Where(f => PossuiHierarquiaCompleta(f, featuresOperador))
                .Select(f => f.Id)
                .ToHashSet();

            if (!featuresOperador.Values.Any(f => f.Codigo == GerenciarPermissoes
                && autoridade.Contains(f.Id)))
                return ResultadoAtualizacaoPermissoes.SemAutoridade;

            var usuario = await new UsuarioRepository(context).ObterPorIdAsync(usuarioId);
            if (usuario is null)
                return ResultadoAtualizacaoPermissoes.UsuarioInexistente;

            var catalogo = (await repository.ListarFeaturesAtivasAsync()).ToDictionary(f => f.Id);
            if (solicitadas.Any(id => !catalogo.ContainsKey(id)))
                return ResultadoAtualizacaoPermissoes.FeaturesInvalidas;

            var atribuidas = await repository.ListarAtribuidasAsync(usuarioId);
            var atuais = atribuidas.Select(f => f.Id).ToHashSet();
            var incluir = solicitadas.Except(atuais).ToArray();
            if (incluir.Any(id => !autoridade.Contains(id)))
                return ResultadoAtualizacaoPermissoes.SemAutoridade;

            var remover = atuais.Where(autoridade.Contains).Except(solicitadas).ToArray();
            var ativasAntes = atribuidas.Where(f => f.Ativo).ToDictionary(f => f.Id);
            var ativasDepois = atuais.Except(remover).Concat(incluir)
                .Where(catalogo.ContainsKey)
                .ToDictionary(id => id, id => catalogo[id]);

            // Alterar um ancestral também pode conceder ou revogar uma permissão fora da autoridade.
            if (ativasAntes.Values.Any(f => !autoridade.Contains(f.Id)
                && PossuiHierarquiaCompleta(f, ativasAntes) != PossuiHierarquiaCompleta(f, ativasDepois)))
                return ResultadoAtualizacaoPermissoes.SemAutoridade;

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
        catch (Exception ex) when (ex.GetBaseException() is PostgresException
            { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.UniqueViolation })
        {
            return ResultadoAtualizacaoPermissoes.AlteracaoSimultanea;
        }
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
