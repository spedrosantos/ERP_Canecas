public class Feature
{
    public long Id { get; set; }
    public long? IdFeaturePai { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}