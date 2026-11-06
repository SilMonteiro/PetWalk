// Cobre a FLX-005: a média do avaliado acumula as avaliações de todos os passeios,
// não apenas as do passeio avaliado no momento.
public class MediaAvaliacaoTests
{
    [Fact]
    public void MediaConsideraAvaliacoesDePasseiosDiferentesDoMesmoPasseador()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

        var primeiro = Fabrica.CriarPasseioSolicitadoPara(passeador);
        var segundo = Fabrica.CriarPasseioSolicitadoPara(passeador);

        Finalizar(primeiro.passeio, passeador);
        Finalizar(segundo.passeio, passeador);

        primeiro.passeio.Avaliar(primeiro.tutor, 5, "Ótimo passeio", Fabrica.Agora);
        segundo.passeio.Avaliar(segundo.tutor, 1, "Passeio ruim", Fabrica.Agora);

        Assert.Equal(3d, passeador.MediaAvaliacao);
    }

    [Fact]
    public void PasseadorSemAvaliacoes_TemMediaZero()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

        Assert.Equal(0d, passeador.MediaAvaliacao);
    }

    private static void Finalizar(Passeio passeio, Usuario passeador)
    {
        passeio.Aceitar(passeador);
        passeio.Iniciar(passeador);
        passeio.Finalizar(passeador);
    }
}
