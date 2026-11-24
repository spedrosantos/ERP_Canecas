using Microsoft.AspNetCore.Authorization;

namespace MS_Identidade.Authorization;

public class FeatureRequirement : IAuthorizationRequirement
{
    public string CodigoFeature { get; }

    public FeatureRequirement(string codigoFeature)
    {
        CodigoFeature = codigoFeature;
    }
}