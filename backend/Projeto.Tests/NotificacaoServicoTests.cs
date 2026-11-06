// Cobre o enviarNotificacao()/listarNaoLidas() da proposta (Etapa 4).
public class NotificacaoServicoTests
{
    private static NotificacaoServico CriarServico() => new(new NotificacaoRepositorioEmMemoria());

    private static (Usuario tutor, Usuario passeador, Passeio passeio) CriarParticipantes()
    {
        var (tutor, passeador, _, passeio) = Fabrica.CriarPasseioSolicitado();
        tutor.DefinirId(1);
        passeador.DefinirId(2);

        return (tutor, passeador, passeio);
    }

    [Fact]
    public void Enviar_GravaNotificacaoNaoLidaParaODestinatario()
    {
        var (_, passeador, passeio) = CriarParticipantes();
        var servico = CriarServico();

        var notificacao = servico.Enviar(passeador, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora);

        Assert.Equal(1, notificacao.Id);
        Assert.False(notificacao.Lida);
        Assert.Same(passeador, notificacao.Destinatario);
        Assert.Same(passeio, notificacao.Passeio);
        Assert.Equal(TipoNotificacao.PASSEIO_SOLICITADO, notificacao.Tipo);
        Assert.Equal(Fabrica.Agora, notificacao.DataEnvio);
        Assert.Single(servico.ListarNaoLidas(passeador.Id));
    }

    [Fact]
    public void ListarPorUsuario_SemFiltro_DevolveLidasENaoLidas()
    {
        var (_, passeador, passeio) = CriarParticipantes();
        var servico = CriarServico();

        var lida = servico.Enviar(passeador, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora);
        servico.Enviar(passeador, passeio, TipoNotificacao.PASSEIO_ACEITO, Fabrica.Agora);
        lida.MarcarComoLida();

        Assert.Equal(2, servico.ListarPorUsuario(passeador.Id, apenasNaoLidas: false).Count);
        Assert.Single(servico.ListarPorUsuario(passeador.Id, apenasNaoLidas: true));
    }

    [Fact]
    public void ListarNaoLidas_NaoDevolveNotificacoesLidas()
    {
        var (_, passeador, passeio) = CriarParticipantes();
        var servico = CriarServico();

        var lida = servico.Enviar(passeador, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora);
        servico.Enviar(passeador, passeio, TipoNotificacao.PASSEIO_ACEITO, Fabrica.Agora);
        lida.MarcarComoLida();

        var naoLidas = servico.ListarNaoLidas(passeador.Id);

        Assert.Single(naoLidas);
        Assert.Equal(TipoNotificacao.PASSEIO_ACEITO, naoLidas[0].Tipo);
    }

    [Fact]
    public void ListarNaoLidas_DevolveApenasAsDoDestinatarioConsultado()
    {
        var (tutor, passeador, passeio) = CriarParticipantes();
        var servico = CriarServico();

        servico.Enviar(tutor, passeio, TipoNotificacao.PASSEIO_CANCELADO, Fabrica.Agora);
        servico.Enviar(passeador, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora);

        var doTutor = servico.ListarNaoLidas(tutor.Id);
        var doPasseador = servico.ListarNaoLidas(passeador.Id);

        Assert.Single(doTutor);
        Assert.Same(tutor, doTutor[0].Destinatario);
        Assert.Single(doPasseador);
        Assert.Same(passeador, doPasseador[0].Destinatario);
    }
}
