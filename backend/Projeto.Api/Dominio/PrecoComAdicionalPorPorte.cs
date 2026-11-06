public class PrecoComAdicionalPorPorte : IPoliticaDePreco
{
    private const decimal FatorPorteGrande = 1.3m;

    private readonly IPoliticaDePreco _base;

    public PrecoComAdicionalPorPorte(IPoliticaDePreco @base)
    {
        _base = @base ?? throw new ArgumentNullException(nameof(@base));
    }

    public decimal Calcular(Passeio passeio)
    {
        if (passeio is null)
        {
            throw new ArgumentNullException(nameof(passeio));
        }

        var valor = _base.Calcular(passeio);

        if (passeio.Pet.Porte == Porte.GRANDE)
        {
            valor *= FatorPorteGrande;
        }

        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}
