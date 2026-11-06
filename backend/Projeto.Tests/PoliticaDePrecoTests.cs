// Exercita as duas políticas pelo mesmo contrato, sem o teste depender da classe concreta.
public class PoliticaDePrecoTests
{
    [Fact]
    public void Politicas_CalculamPeloMesmoContrato()
    {
        IPoliticaDePreco porHora = new PrecoPorHora();
        IPoliticaDePreco comAdicional = new PrecoComAdicionalPorPorte(porHora);

        var (_, _, _, passeio) = Fabrica.CriarPasseioSolicitado(TimeSpan.FromHours(1), Porte.GRANDE, 30m);

        Assert.Equal(30m, porHora.Calcular(passeio));
        Assert.Equal(39m, comAdicional.Calcular(passeio));
    }

    [Theory]
    [InlineData(60, 30, 30)]
    [InlineData(90, 40, 60)]
    [InlineData(30, 50, 25)]
    public void PrecoPorHora_MultiplicaDuracaoEmHorasPeloValorHora(int minutos, int valorHora, int esperado)
    {
        IPoliticaDePreco politica = new PrecoPorHora();
        var (_, _, _, passeio) = Fabrica.CriarPasseioSolicitado(TimeSpan.FromMinutes(minutos), Porte.MEDIO, valorHora);

        Assert.Equal((decimal)esperado, politica.Calcular(passeio));
    }

    [Fact]
    public void PrecoPorHora_ArredondaParaDuasCasasAfastandoDoPontoMedio()
    {
        IPoliticaDePreco politica = new PrecoPorHora();
        var (_, _, _, passeio) = Fabrica.CriarPasseioSolicitado(TimeSpan.FromHours(1), Porte.MEDIO, 10.005m);

        Assert.Equal(10.01m, politica.Calcular(passeio));
    }

    [Theory]
    [InlineData(Porte.GRANDE, 39)]
    [InlineData(Porte.MEDIO, 30)]
    [InlineData(Porte.PEQUENO, 30)]
    public void PrecoComAdicionalPorPorte_UsaOPrecoPorHoraComoBase(Porte porte, int esperado)
    {
        IPoliticaDePreco politica = new PrecoComAdicionalPorPorte(new PrecoPorHora());
        var (_, _, _, passeio) = Fabrica.CriarPasseioSolicitado(TimeSpan.FromHours(1), porte, 30m);

        Assert.Equal((decimal)esperado, politica.Calcular(passeio));
    }

    [Theory]
    [InlineData(Porte.GRANDE, 130)]
    [InlineData(Porte.MEDIO, 100)]
    public void PrecoComAdicionalPorPorte_UsaABaseRecebida(Porte porte, int esperado)
    {
        IPoliticaDePreco politica = new PrecoComAdicionalPorPorte(new PoliticaDePrecoFalsa());
        var (_, _, _, passeio) = Fabrica.CriarPasseioSolicitado(TimeSpan.FromHours(1), porte, 30m);

        Assert.Equal((decimal)esperado, politica.Calcular(passeio));
    }

    [Fact]
    public void PrecoPorHora_ComPasseioNulo_LancaErro()
    {
        IPoliticaDePreco politica = new PrecoPorHora();

        Assert.Throws<ArgumentNullException>(() => politica.Calcular(null!));
    }

    [Fact]
    public void PrecoComAdicionalPorPorte_ComPasseioNulo_LancaErro()
    {
        IPoliticaDePreco politica = new PrecoComAdicionalPorPorte(new PrecoPorHora());

        Assert.Throws<ArgumentNullException>(() => politica.Calcular(null!));
    }

    [Fact]
    public void PrecoComAdicionalPorPorte_ComBaseNula_LancaErro()
    {
        Assert.Throws<ArgumentNullException>(() => new PrecoComAdicionalPorPorte(null!));
    }

    private sealed class PoliticaDePrecoFalsa : IPoliticaDePreco
    {
        public decimal Calcular(Passeio passeio) => 100m;
    }
}
