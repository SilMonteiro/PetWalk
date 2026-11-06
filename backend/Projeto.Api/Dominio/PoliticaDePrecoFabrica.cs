public static class PoliticaDePrecoFabrica
{
    public static IPoliticaDePreco Para(TipoCobranca tipo)
    {
        return tipo switch
        {
            TipoCobranca.POR_HORA => new PrecoPorHora(),
            TipoCobranca.POR_HORA_COM_ADICIONAL_DE_PORTE =>
                new PrecoComAdicionalPorPorte(new PrecoPorHora()),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo))
        };
    }
}
