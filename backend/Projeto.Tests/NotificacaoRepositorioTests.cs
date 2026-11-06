// INotificacaoRepositorio: busca por Id, usada depois pelo endpoint de "marcar como lida".
public class NotificacaoRepositorioTests
{
    [Fact]
    public void ObterPorId_DevolveANotificacaoCerta()
    {
        var (tutor, _, _, passeio) = Fabrica.CriarPasseioSolicitado();
        tutor.DefinirId(1);

        var repositorio = new NotificacaoRepositorioEmMemoria();
        repositorio.Adicionar(new Notificacao(tutor, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora));
        var segunda = repositorio.Adicionar(new Notificacao(tutor, passeio, TipoNotificacao.PASSEIO_ACEITO, Fabrica.Agora));

        Assert.Same(segunda, repositorio.ObterPorId(2));
        Assert.Equal(TipoNotificacao.PASSEIO_ACEITO, repositorio.ObterPorId(2)!.Tipo);
    }

    [Fact]
    public void ObterPorId_Inexistente_DevolveNulo()
    {
        var repositorio = new NotificacaoRepositorioEmMemoria();

        Assert.Null(repositorio.ObterPorId(99));
    }
}
