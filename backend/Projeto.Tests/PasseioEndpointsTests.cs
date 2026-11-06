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
    }

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
}
