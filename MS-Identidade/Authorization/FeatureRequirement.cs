using Microsoft.AspNetCore.Authorization;

namespace MS_Identidade.Authorization;

public class FeatureRequirement : IAuthorizationRequirement
{
    public string CodigoFeature { get; }
    public IReadOnlyList<string> Alternativas { get; }

    public FeatureRequirement(string codigoFeature, params string[] alternativas)
    {
        CodigoFeature = codigoFeature;
        Alternativas = alternativas;
    }
}
