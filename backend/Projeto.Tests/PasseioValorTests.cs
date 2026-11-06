// Garante que o valor do passeio é calculado uma única vez, na solicitação, pela política escolhida.
public class PasseioValorTests
{
    [Fact]
    public void Passeio_PorHora_CalculaPelaDuracaoEValorHora()
    {
        var (_, _, _, passeio) = Fabrica.CriarPasseioSolicitado(TimeSpan.FromMinutes(90), Porte.MEDIO, 40m);

        Assert.Equal(60m, passeio.Valor);
    }

    [Fact]
    public void Passeio_ComAdicionalPorPorte_CobraAdicionalDePetGrande()
    {
        var passeio = CriarPasseio(Porte.GRANDE, TipoCobranca.POR_HORA_COM_ADICIONAL_DE_PORTE, 30m, TimeSpan.FromHours(1));

        Assert.Equal(39m, passeio.Valor);
    }

    [Fact]
    public void Passeio_ComAdicionalPorPorte_NaoCobraAdicionalDePetMedio()
    {
        var passeio = CriarPasseio(Porte.MEDIO, TipoCobranca.POR_HORA_COM_ADICIONAL_DE_PORTE, 30m, TimeSpan.FromHours(1));

        Assert.Equal(30m, passeio.Valor);
    }

    [Fact]
    public void Passeio_SemCobrancaConfigurada_TemValorZero()
    {
        var (_, _, _, passeio) = Fabrica.CriarPasseioSolicitado();

        Assert.Equal(0m, passeio.Valor);
    }

    [Fact]
    public void Passeio_MantemOValorDepoisDeMudarOValorHoraDoPasseador()
    {
        var (_, passeador, _, passeio) = Fabrica.CriarPasseioSolicitado(TimeSpan.FromMinutes(90), Porte.MEDIO, 40m);

        passeador.DefinirCobranca(100m, TipoCobranca.POR_HORA);

        Assert.Equal(60m, passeio.Valor);
    }

    [Fact]
    public void Fabrica_DevolveAPoliticaCorretaParaCadaTipoCobranca()
    {
        Assert.IsType<PrecoPorHora>(PoliticaDePrecoFabrica.Para(TipoCobranca.POR_HORA));
        Assert.IsType<PrecoComAdicionalPorPorte>(
            PoliticaDePrecoFabrica.Para(TipoCobranca.POR_HORA_COM_ADICIONAL_DE_PORTE));
    }

    [Fact]
    public void Fabrica_ComTipoForaDoEnum_LancaErro()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PoliticaDePrecoFabrica.Para((TipoCobranca)99));
    }

    private static Passeio CriarPasseio(Porte porte, TipoCobranca tipoCobranca, decimal valorHora, TimeSpan duracao)
    {
        var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);
        passeador.DefinirCobranca(valorHora, tipoCobranca);

        var pet = Fabrica.CriarPet(tutor, porte);
        tutor.AdicionarPet(pet);

        return tutor.SolicitarPasseio(
            pet, passeador, Fabrica.Agora.AddDays(1), duracao, "Rua A, 10", Fabrica.Agora);
    }
}
