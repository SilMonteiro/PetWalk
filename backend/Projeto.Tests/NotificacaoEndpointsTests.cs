using Microsoft.AspNetCore.Http;

// Listagem das notificações do destinatário e marcação como lida (listarNaoLidas/marcarComoLida).
public class NotificacaoEndpointsTests
{
    private static (IUsuarioRepositorio usuarios, INotificacaoRepositorio notificacoes, NotificacaoServico servico,
        Usuario tutor, Usuario passeador, Passeio passeio) CriarCenario()
    {
        var usuarios = new UsuarioRepositorioEmMemoria();
        var notificacoes = new NotificacaoRepositorioEmMemoria();
        var servico = new NotificacaoServico(notificacoes);

        var tutor = usuarios.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR));
        var passeador = usuarios.Adicionar(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "passeador@petwalk.com"));
        var pet = usuarios.RegistrarPet(tutor, Fabrica.CriarPet(tutor));
        var passeio = tutor.SolicitarPasseio(pet, passeador, Fabrica.Agora.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10", Fabrica.Agora);

        return (usuarios, notificacoes, servico, tutor, passeador, passeio);
    }

    [Fact]
    public void Listar_NaoLidas_DevolveSoAsPendentesDoDestinatario()
    {
        var (usuarios, notificacoes, servico, tutor, passeador, passeio) = CriarCenario();
        servico.Enviar(passeador, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora);
        var aceita = servico.Enviar(tutor, passeio, TipoNotificacao.PASSEIO_ACEITO, Fabrica.Agora);
        aceita.MarcarComoLida();

        var doPasseador = ListaDe(NotificacaoEndpoints.Listar(passeador.Id, true, usuarios, servico));
        var doTutor = ListaDe(NotificacaoEndpoints.Listar(tutor.Id, true, usuarios, servico));

        var pendente = Assert.Single(doPasseador);
        Assert.Equal(TipoNotificacao.PASSEIO_SOLICITADO, pendente.Tipo);
        Assert.Equal(passeio.Id, pendente.PasseioId);
        Assert.False(pendente.Lida);

        Assert.Empty(doTutor);
        Assert.Single(notificacoes.ListarPorDestinatario(tutor.Id));
    }

    [Fact]
    public void Listar_SemFiltro_DevolveTambemAsLidas()
    {
        var (usuarios, _, servico, tutor, _, passeio) = CriarCenario();
        var notificacao = servico.Enviar(tutor, passeio, TipoNotificacao.PASSEIO_ACEITO, Fabrica.Agora);
        notificacao.MarcarComoLida();

        var doTutor = ListaDe(NotificacaoEndpoints.Listar(tutor.Id, false, usuarios, servico));

        Assert.True(Assert.Single(doTutor).Lida);
    }

    [Fact]
    public void Listar_UsuarioInexistente_Devolve404()
    {
        var (usuarios, _, servico, _, _, _) = CriarCenario();

        var resultado = NotificacaoEndpoints.Listar(999, true, usuarios, servico);

        Assert.Equal(404, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
        Assert.Equal("Usuário não encontrado", MensagemDe(resultado));
    }

    [Fact]
    public void MarcarComoLida_Destinatario_Devolve204()
    {
        var (_, notificacoes, servico, _, passeador, passeio) = CriarCenario();
        var notificacao = servico.Enviar(passeador, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora);

        var resultado = NotificacaoEndpoints.MarcarComoLida(
            notificacao.Id, new MarcarNotificacaoLidaRequest(passeador.Id), notificacoes);

        Assert.Equal(204, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
        Assert.True(notificacao.Lida);
    }

    [Fact]
    public void MarcarComoLida_OutroUsuario_Devolve404SemMarcar()
    {
        var (_, notificacoes, servico, tutor, passeador, passeio) = CriarCenario();
        var notificacao = servico.Enviar(passeador, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora);

        var resultado = NotificacaoEndpoints.MarcarComoLida(
            notificacao.Id, new MarcarNotificacaoLidaRequest(tutor.Id), notificacoes);

        Assert.Equal(404, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
        Assert.Equal("Notificação não encontrada", MensagemDe(resultado));
        Assert.False(notificacao.Lida);
    }

    [Fact]
    public void MarcarComoLida_Inexistente_Devolve404()
    {
        var (_, notificacoes, _, _, passeador, _) = CriarCenario();

        var resultado = NotificacaoEndpoints.MarcarComoLida(
            999, new MarcarNotificacaoLidaRequest(passeador.Id), notificacoes);

        Assert.Equal(404, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
        Assert.Equal("Notificação não encontrada", MensagemDe(resultado));
    }

    private static List<NotificacaoResponse> ListaDe(IResult resultado) =>
        Assert.IsType<List<NotificacaoResponse>>(Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value);

    private static string? MensagemDe(IResult resultado)
    {
        var corpo = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;

        return corpo?.GetType().GetProperty("erro")?.GetValue(corpo) as string;
    }
}
