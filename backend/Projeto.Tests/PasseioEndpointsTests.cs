using System.Text.Json;
using Microsoft.AspNetCore.Http;

// FLX-002: fluxo completo da solicitação — grava o passeio e notifica o passeador escolhido.
public class PasseioEndpointsTests
{
    private static (IUsuarioRepositorio usuarios, IPasseioRepositorio passeios, INotificacaoRepositorio notificacoes,
        NotificacaoServico servico, Usuario tutor, Pet pet, Usuario passeador) CriarCenario()
    {
        var usuarios = new UsuarioRepositorioEmMemoria();
        var passeios = new PasseioRepositorioEmMemoria();
        var notificacoes = new NotificacaoRepositorioEmMemoria();

        var tutor = usuarios.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR));
        var passeador = usuarios.Adicionar(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "passeador@petwalk.com"));
        var pet = usuarios.RegistrarPet(tutor, Fabrica.CriarPet(tutor));

        return (usuarios, passeios, notificacoes, new NotificacaoServico(notificacoes), tutor, pet, passeador);
    }

    private static SolicitacaoPasseioRequest Requisicao(int usuarioId, int petId, int passeadorId, string local = "Rua A, 10") =>
        new(usuarioId, petId, passeadorId, Fabrica.Agora.AddDays(1), 60, local);

    [Fact]
    public void Solicitar_GravaOPasseioENotificaOPasseador()
    {
        var (usuarios, passeios, notificacoes, servico, tutor, pet, passeador) = CriarCenario();

        var resultado = PasseioEndpoints.Solicitar(Requisicao(tutor.Id, pet.Id, passeador.Id), usuarios, passeios, servico, Fabrica.Agora);

        Assert.Equal(201, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);

        var passeio = Assert.Single(passeios.ListarPorUsuario(tutor.Id));
        Assert.Equal(StatusPasseio.SOLICITADO, passeio.Status);
        Assert.Same(tutor, passeio.Tutor);
        Assert.Same(passeador, passeio.Passeador);
        Assert.Equal("Rua A, 10", passeio.Local);

        var notificacao = Assert.Single(notificacoes.ListarPorDestinatario(passeador.Id));
        Assert.Equal(TipoNotificacao.PASSEIO_SOLICITADO, notificacao.Tipo);
        Assert.Same(passeio, notificacao.Passeio);
        Assert.False(notificacao.Lida);
    }

    [Fact]
    public void Solicitar_Resposta_NaoVazaEnderecoDoPerfil()
    {
        var (usuarios, passeios, _, servico, tutor, pet, passeador) = CriarCenario();

        var resultado = PasseioEndpoints.Solicitar(Requisicao(tutor.Id, pet.Id, passeador.Id), usuarios, passeios, servico, Fabrica.Agora);

        var resposta = Assert.IsType<PasseioResponse>(
            Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value);

        // REG-007: o local é o informado na solicitação, não o endereço do perfil.
        Assert.Equal("Rua A, 10", resposta.Local);
        Assert.Equal(tutor.Nome, resposta.Tutor.Nome);
        Assert.Equal(passeador.Nome, resposta.Passeador.Nome);
        Assert.Equal("Rex", resposta.NomePet);
        Assert.Equal(60, resposta.DuracaoMinutos);
        Assert.Equal("TUTOR", resposta.Papel);
    }

    [Fact]
    public void Listar_TutorEPasseadorVeemOMesmoPasseioComPapeisDiferentes()
    {
        var (usuarios, passeios, _, servico, tutor, pet, passeador) = CriarCenario();
        PasseioEndpoints.Solicitar(Requisicao(tutor.Id, pet.Id, passeador.Id), usuarios, passeios, servico, Fabrica.Agora);

        var doTutor = ListaDe(PasseioEndpoints.Listar(tutor.Id, usuarios, passeios));
        var doPasseador = ListaDe(PasseioEndpoints.Listar(passeador.Id, usuarios, passeios));

        var paraTutor = Assert.Single(doTutor);
        var paraPasseador = Assert.Single(doPasseador);

        Assert.Equal(paraTutor.Id, paraPasseador.Id);
        Assert.Equal("TUTOR", paraTutor.Papel);
        Assert.Equal("PASSEADOR", paraPasseador.Papel);
    }

    [Fact]
    public void Listar_NaoExpoeDadosSensiveis()
    {
        var (usuarios, passeios, _, servico, tutor, pet, passeador) = CriarCenario();
        PasseioEndpoints.Solicitar(Requisicao(tutor.Id, pet.Id, passeador.Id), usuarios, passeios, servico, Fabrica.Agora);

        var json = JsonSerializer.Serialize(
                ListaDe(PasseioEndpoints.Listar(tutor.Id, usuarios, passeios)),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .ToLowerInvariant();

        Assert.DoesNotContain("endereco", json);
        Assert.DoesNotContain("email", json);
        Assert.DoesNotContain("telefone", json);
        Assert.DoesNotContain("senha", json);
    }

    [Fact]
    public void Listar_UsuarioInexistente_Devolve404()
    {
        var (usuarios, passeios, _, _, _, _, _) = CriarCenario();

        var resultado = PasseioEndpoints.Listar(999, usuarios, passeios);

        Assert.Equal(404, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
    }

    private static List<PasseioResponse> ListaDe(IResult resultado) =>
        Assert.IsType<List<PasseioResponse>>(Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value);

    [Fact]
    public void Solicitar_TutorInexistente_Devolve404()
    {
        var (usuarios, passeios, _, servico, _, pet, passeador) = CriarCenario();

        var resultado = PasseioEndpoints.Solicitar(Requisicao(999, pet.Id, passeador.Id), usuarios, passeios, servico, Fabrica.Agora);

        Assert.Equal(404, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);

        var resposta = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;
        Assert.Equal("Usuário não encontrado", resposta?.GetType().GetProperty("erro")?.GetValue(resposta));
    }

    [Fact]
    public void Solicitar_PasseadorInexistente_Devolve404()
    {
        var (usuarios, passeios, _, servico, tutor, pet, _) = CriarCenario();

        var resultado = PasseioEndpoints.Solicitar(Requisicao(tutor.Id, pet.Id, 999), usuarios, passeios, servico, Fabrica.Agora);

        Assert.Equal(404, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);

        var resposta = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;
        Assert.Equal("Passeador não encontrado", resposta?.GetType().GetProperty("erro")?.GetValue(resposta));
    }

    [Fact]
    public void Solicitar_PetDeOutroTutor_Devolve422ComAMensagemDaREG005()
    {
        var (usuarios, passeios, _, servico, tutor, _, passeador) = CriarCenario();
        var outroTutor = usuarios.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR, "outro@petwalk.com"));
        var petDoOutro = usuarios.RegistrarPet(outroTutor, Fabrica.CriarPet(outroTutor));

        var resultado = PasseioEndpoints.Solicitar(Requisicao(tutor.Id, petDoOutro.Id, passeador.Id), usuarios, passeios, servico, Fabrica.Agora);

        Assert.Equal(422, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);

        var resposta = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;
        Assert.Equal("O pet informado não pertence ao tutor solicitante", resposta?.GetType().GetProperty("erro")?.GetValue(resposta));
        Assert.Empty(passeios.ListarPorUsuario(tutor.Id));
    }

    [Fact]
    public void Aceitar_PasseadorCerto_NotificaOTutor()
    {
        var (usuarios, passeios, notificacoes, servico, tutor, pet, passeador) = CriarCenario();
        var passeio = CriarPasseioPersistido(usuarios, passeios, servico, tutor, pet, passeador);

        var resultado = ExecutarAcao(passeio.Id, passeador, passeios, usuarios, servico,
            (p, quem) => p.Aceitar(quem), TipoNotificacao.PASSEIO_ACEITO);

        Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
        Assert.Equal(StatusPasseio.ACEITO, passeio.Status);

        var notificacao = Assert.Single(notificacoes.ListarPorDestinatario(tutor.Id));
        Assert.Equal(TipoNotificacao.PASSEIO_ACEITO, notificacao.Tipo);
        Assert.Same(passeio, notificacao.Passeio);
    }

    [Fact]
    public void Recusar_PasseadorCerto_NotificaOTutor()
    {
        var (usuarios, passeios, notificacoes, servico, tutor, pet, passeador) = CriarCenario();
        var passeio = CriarPasseioPersistido(usuarios, passeios, servico, tutor, pet, passeador);

        var resultado = ExecutarAcao(passeio.Id, passeador, passeios, usuarios, servico,
            (p, quem) => p.Recusar(quem), TipoNotificacao.PASSEIO_RECUSADO);

        Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
        Assert.Equal(StatusPasseio.RECUSADO, passeio.Status);

        var notificacao = Assert.Single(notificacoes.ListarPorDestinatario(tutor.Id));
        Assert.Equal(TipoNotificacao.PASSEIO_RECUSADO, notificacao.Tipo);
    }

    [Fact]
    public void Aceitar_PasseadorErrado_Devolve422SemNotificar()
    {
        var (usuarios, passeios, notificacoes, servico, tutor, pet, passeador) = CriarCenario();
        var passeio = CriarPasseioPersistido(usuarios, passeios, servico, tutor, pet, passeador);
        var outroPasseador = usuarios.Adicionar(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "outro-passeador@petwalk.com"));

        var resultado = ExecutarAcao(passeio.Id, outroPasseador, passeios, usuarios, servico,
            (p, quem) => p.Aceitar(quem), TipoNotificacao.PASSEIO_ACEITO);

        Assert.Equal(422, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);

        var resposta = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;
        Assert.Equal("Passeio não pertence a este passeador", resposta?.GetType().GetProperty("erro")?.GetValue(resposta));

        Assert.Equal(StatusPasseio.SOLICITADO, passeio.Status);
        Assert.Empty(notificacoes.ListarPorDestinatario(tutor.Id));
    }

    [Fact]
    public void Aceitar_PasseioInexistente_Devolve404()
    {
        var (usuarios, passeios, _, servico, tutor, pet, passeador) = CriarCenario();
        CriarPasseioPersistido(usuarios, passeios, servico, tutor, pet, passeador);

        var resultado = ExecutarAcao(999, passeador, passeios, usuarios, servico,
            (p, quem) => p.Aceitar(quem), TipoNotificacao.PASSEIO_ACEITO);

        Assert.Equal(404, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);

        var resposta = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;
        Assert.Equal("Passeio não encontrado", resposta?.GetType().GetProperty("erro")?.GetValue(resposta));
    }

    [Fact]
    public void Iniciar_AposAceite_NotificaOTutor()
    {
        var (usuarios, passeios, notificacoes, servico, tutor, pet, passeador) = CriarCenario();
        var passeio = CriarPasseioPersistido(usuarios, passeios, servico, tutor, pet, passeador);
        passeio.Aceitar(passeador);

        var resultado = ExecutarAcao(passeio.Id, passeador, passeios, usuarios, servico,
            (p, quem) => p.Iniciar(quem), TipoNotificacao.PASSEIO_INICIADO);

        Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
        Assert.Equal(StatusPasseio.EM_ANDAMENTO, passeio.Status);

        var notificacao = Assert.Single(notificacoes.ListarPorDestinatario(tutor.Id));
        Assert.Equal(TipoNotificacao.PASSEIO_INICIADO, notificacao.Tipo);
    }

    [Fact]
    public void Finalizar_EmAndamento_NotificaOTutor()
    {
        var (usuarios, passeios, notificacoes, servico, tutor, pet, passeador) = CriarCenario();
        var passeio = CriarPasseioPersistido(usuarios, passeios, servico, tutor, pet, passeador);
        passeio.Aceitar(passeador);
        passeio.Iniciar(passeador);

        var resultado = ExecutarAcao(passeio.Id, passeador, passeios, usuarios, servico,
            (p, quem) => p.Finalizar(quem), TipoNotificacao.PASSEIO_FINALIZADO);

        Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
        Assert.Equal(StatusPasseio.FINALIZADO, passeio.Status);

        var notificacao = Assert.Single(notificacoes.ListarPorDestinatario(tutor.Id));
        Assert.Equal(TipoNotificacao.PASSEIO_FINALIZADO, notificacao.Tipo);
    }

    [Fact]
    public void Iniciar_SemAceite_Devolve422SemNotificar()
    {
        var (usuarios, passeios, notificacoes, servico, tutor, pet, passeador) = CriarCenario();
        var passeio = CriarPasseioPersistido(usuarios, passeios, servico, tutor, pet, passeador);

        var resultado = ExecutarAcao(passeio.Id, passeador, passeios, usuarios, servico,
            (p, quem) => p.Iniciar(quem), TipoNotificacao.PASSEIO_INICIADO);

        Assert.Equal(422, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
        Assert.Equal(StatusPasseio.SOLICITADO, passeio.Status);
        Assert.Empty(notificacoes.ListarPorDestinatario(tutor.Id));
    }

    [Fact]
    public void Cancelar_PeloTutor_NotificaOPasseador()
    {
        var (usuarios, passeios, notificacoes, servico, tutor, pet, passeador) = CriarCenario();
        var passeio = CriarPasseioPersistido(usuarios, passeios, servico, tutor, pet, passeador);

        var resultado = ExecutarAcao(passeio.Id, tutor, passeios, usuarios, servico,
            (p, quem) => p.Cancelar(quem), TipoNotificacao.PASSEIO_CANCELADO);

        Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);
        Assert.Equal(StatusPasseio.CANCELADO, passeio.Status);

        // O passeador já tinha a notificação da solicitação; a do cancelamento é a mais nova.
        var notificacao = Assert.Single(
            notificacoes.ListarPorDestinatario(passeador.Id),
            n => n.Tipo == TipoNotificacao.PASSEIO_CANCELADO);
        Assert.Same(passeio, notificacao.Passeio);
        Assert.Empty(notificacoes.ListarPorDestinatario(tutor.Id));
    }

    [Fact]
    public void Cancelar_EmAndamento_Devolve422SemNotificar()
    {
        var (usuarios, passeios, notificacoes, servico, tutor, pet, passeador) = CriarCenario();
        var passeio = CriarPasseioPersistido(usuarios, passeios, servico, tutor, pet, passeador);
        passeio.Aceitar(passeador);
        passeio.Iniciar(passeador);

        var resultado = ExecutarAcao(passeio.Id, tutor, passeios, usuarios, servico,
            (p, quem) => p.Cancelar(quem), TipoNotificacao.PASSEIO_CANCELADO);

        Assert.Equal(422, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);

        var resposta = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;
        Assert.Equal("Passeio não pode ser cancelado no estado atual.", resposta?.GetType().GetProperty("erro")?.GetValue(resposta));

        Assert.Equal(StatusPasseio.EM_ANDAMENTO, passeio.Status);
        Assert.DoesNotContain(
            notificacoes.ListarPorDestinatario(passeador.Id), n => n.Tipo == TipoNotificacao.PASSEIO_CANCELADO);
    }

    private static Passeio CriarPasseioPersistido(
        IUsuarioRepositorio usuarios, IPasseioRepositorio passeios, NotificacaoServico servico,
        Usuario tutor, Pet pet, Usuario passeador)
    {
        PasseioEndpoints.Solicitar(Requisicao(tutor.Id, pet.Id, passeador.Id), usuarios, passeios, servico, Fabrica.Agora);

        return Assert.Single(passeios.ListarPorUsuario(tutor.Id));
    }

    private static IResult ExecutarAcao(int passeioId, Usuario quem, IPasseioRepositorio passeios,
        IUsuarioRepositorio usuarios, NotificacaoServico servico, Action<Passeio, Usuario> acao, TipoNotificacao tipo) =>
        PasseioEndpoints.ExecutarAcao(passeioId, new AcaoPasseioRequest(quem.Id), passeios, usuarios, servico, Fabrica.Agora, acao, tipo);
}
