public class PrecoPorHora : IPoliticaDePreco
{
    public decimal Calcular(Passeio passeio)
    {
        if (passeio is null)
        {
            throw new ArgumentNullException(nameof(passeio));
        }

        var valor = (decimal)passeio.Duracao.TotalHours * passeio.Passeador.ValorHora;

        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}
