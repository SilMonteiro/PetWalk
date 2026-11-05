// Cobre a lógica que veio de Tutor.SolicitarPasseio e Passeador.AceitarPasseio,
// depois que as duas classes legadas foram removidas (REG-002, 003, 005, 008 e 009).
public class SolicitacaoDePasseioTests
{
    private static Usuario CriarUsuario(TipoPerfil tipoPerfil) =>
        new("Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", tipoPerfil, "hash-irrelevante");

    private static Pet CriarPet(Usuario tutor) =>
        new(tutor, "Rex", "Cachorro", "Vira-lata", Porte.MEDIO, 3, "Muito dócil");

    [Fact]
    public void TutorComPetVinculado_SolicitaPasseio_CriaPasseioSolicitado()
    {
        var tutor = CriarUsuario(TipoPerfil.TUTOR);
        var passeador = CriarUsuario(TipoPerfil.PASSEADOR);
        var pet = CriarPet(tutor);
        tutor.AdicionarPet(pet);

        var passeio = tutor.SolicitarPasseio(pet, passeador, Fabrica.Agora.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10", Fabrica.Agora);

        Assert.Equal(StatusPasseio.SOLICITADO, passeio.Status);
        Assert.Same(tutor, passeio.Tutor);
        Assert.Same(passeador, passeio.Passeador);
        Assert.Same(pet, passeio.Pet);
        Assert.Contains(pet, tutor.Pets);
    }

    [Fact]
    public void UsuarioSemPerfilDeTutor_NaoPodeSolicitar()
    {
        var passeador = CriarUsuario(TipoPerfil.PASSEADOR);
        var tutor = CriarUsuario(TipoPerfil.TUTOR);
        var pet = CriarPet(tutor);

        var erro = Assert.Throws<InvalidOperationException>(
            () => passeador.SolicitarPasseio(pet, passeador, Fabrica.Agora.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10", Fabrica.Agora));

        Assert.Equal("Usuário não possui perfil de tutor", erro.Message);
    }

    [Fact]
    public void PetDeOutroTutor_NaoPodeSerSolicitado()
    {
        var tutor = CriarUsuario(TipoPerfil.TUTOR);
        var outroTutor = CriarUsuario(TipoPerfil.TUTOR);
        var passeador = CriarUsuario(TipoPerfil.PASSEADOR);
        var petDoOutro = CriarPet(outroTutor);

        var erro = Assert.Throws<InvalidOperationException>(
            () => tutor.SolicitarPasseio(petDoOutro, passeador, Fabrica.Agora.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10", Fabrica.Agora));

        Assert.Equal("O pet informado não pertence ao tutor solicitante", erro.Message);
    }

    [Fact]
    public void DataNoPassado_NaoPodeSerSolicitada()
    {
        var tutor = CriarUsuario(TipoPerfil.TUTOR);
        var passeador = CriarUsuario(TipoPerfil.PASSEADOR);
        var pet = CriarPet(tutor);
        tutor.AdicionarPet(pet);

        var erro = Assert.Throws<InvalidOperationException>(
            () => tutor.SolicitarPasseio(pet, passeador, Fabrica.Agora.AddDays(-1), TimeSpan.FromHours(1), "Rua A, 10", Fabrica.Agora));

        Assert.Equal("A data e hora do passeio devem ser futuras", erro.Message);
    }

    [Fact]
    public void DataIgualAAgora_NaoPodeSerSolicitada()
    {
        var tutor = CriarUsuario(TipoPerfil.TUTOR);
        var passeador = CriarUsuario(TipoPerfil.PASSEADOR);
        var pet = CriarPet(tutor);
        tutor.AdicionarPet(pet);

        var erro = Assert.Throws<InvalidOperationException>(
            () => tutor.SolicitarPasseio(pet, passeador, Fabrica.Agora, TimeSpan.FromHours(1), "Rua A, 10", Fabrica.Agora));

        Assert.Equal("A data e hora do passeio devem ser futuras", erro.Message);
    }

    [Fact]
    public void PasseadorEscolhidoSemPerfilDePasseador_NaoPodeReceberSolicitacao()
    {
        var tutor = CriarUsuario(TipoPerfil.TUTOR);
        var outroTutor = CriarUsuario(TipoPerfil.TUTOR);
        var pet = CriarPet(tutor);
        tutor.AdicionarPet(pet);

        var erro = Assert.Throws<InvalidOperationException>(
            () => tutor.SolicitarPasseio(pet, outroTutor, Fabrica.Agora.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10", Fabrica.Agora));

        Assert.Equal("Usuário não possui perfil de passeador", erro.Message);
    }

    [Fact]
    public void PasseadorEscolhido_AceitaPasseio()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();

        passeio.Aceitar(passeador);

        Assert.Equal(StatusPasseio.ACEITO, passeio.Status);
    }

    [Fact]
    public void UsuarioQueNaoEhOPasseador_NaoPodeAceitar()
    {
        var (passeio, _) = CriarPasseioSolicitado();
        var estranho = CriarUsuario(TipoPerfil.PASSEADOR);

        var erro = Assert.Throws<InvalidOperationException>(() => passeio.Aceitar(estranho));

        Assert.Equal("Passeio não pertence a este passeador", erro.Message);
    }

    [Fact]
    public void PasseioJaAceito_NaoPodeSerAceitoDeNovo()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();
        passeio.Aceitar(passeador);

        var erro = Assert.Throws<InvalidOperationException>(() => passeio.Aceitar(passeador));

        Assert.Equal("Transição não permitida a partir do estado atual", erro.Message);
    }

    [Fact]
    public void PasseadorEscolhido_RecusaPasseio()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();

        passeio.Recusar(passeador);

        Assert.Equal(StatusPasseio.RECUSADO, passeio.Status);
    }

    [Fact]
    public void UsuarioQueNaoEhOPasseador_NaoPodeRecusar()
    {
        var (passeio, _) = CriarPasseioSolicitado();
        var estranho = CriarUsuario(TipoPerfil.PASSEADOR);

        var erro = Assert.Throws<InvalidOperationException>(() => passeio.Recusar(estranho));

        Assert.Equal("Passeio não pertence a este passeador", erro.Message);
    }

    [Fact]
    public void PasseadorBloqueado_NaoPodeRecusar()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();
        passeador.Bloquear();

        var erro = Assert.Throws<InvalidOperationException>(() => passeio.Recusar(passeador));

        Assert.Equal("Usuário está bloqueado", erro.Message);
        Assert.Equal(StatusPasseio.SOLICITADO, passeio.Status);
    }

    [Fact]
    public void PasseadorBloqueado_NaoPodeAceitarIniciarNemFinalizar()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();
        passeador.Bloquear();

        var erroAceitar = Assert.Throws<InvalidOperationException>(() => passeio.Aceitar(passeador));
        Assert.Equal("Usuário está bloqueado", erroAceitar.Message);
        Assert.Equal(StatusPasseio.SOLICITADO, passeio.Status);

        passeador.Desbloquear();
        passeio.Aceitar(passeador);
        passeador.Bloquear();

        var erroIniciar = Assert.Throws<InvalidOperationException>(() => passeio.Iniciar(passeador));
        Assert.Equal("Usuário está bloqueado", erroIniciar.Message);
        Assert.Equal(StatusPasseio.ACEITO, passeio.Status);

        passeador.Desbloquear();
        passeio.Iniciar(passeador);
        passeador.Bloquear();

        var erroFinalizar = Assert.Throws<InvalidOperationException>(() => passeio.Finalizar(passeador));
        Assert.Equal("Usuário está bloqueado", erroFinalizar.Message);
        Assert.Equal(StatusPasseio.EM_ANDAMENTO, passeio.Status);
    }

    [Fact]
    public void PasseioJaAceito_NaoPodeSerRecusado()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();
        passeio.Aceitar(passeador);

        var erro = Assert.Throws<InvalidOperationException>(() => passeio.Recusar(passeador));

        Assert.Equal("Transição não permitida a partir do estado atual", erro.Message);
        Assert.Equal(StatusPasseio.ACEITO, passeio.Status);
    }

    [Fact]
    public void PasseioRecusado_NaoPermiteNovasTransicoes()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();
        passeio.Recusar(passeador);

        Assert.Throws<InvalidOperationException>(() => passeio.Aceitar(passeador));
        Assert.Throws<InvalidOperationException>(() => passeio.Recusar(passeador));
        Assert.Equal(StatusPasseio.RECUSADO, passeio.Status);
    }

    [Fact]
    public void CicloCompleto_SolicitadoAceitoEmAndamentoFinalizado()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();
        Assert.Equal(StatusPasseio.SOLICITADO, passeio.Status);

        passeio.Aceitar(passeador);
        Assert.Equal(StatusPasseio.ACEITO, passeio.Status);

        passeio.Iniciar(passeador);
        Assert.Equal(StatusPasseio.EM_ANDAMENTO, passeio.Status);

        passeio.Finalizar(passeador);
        Assert.Equal(StatusPasseio.FINALIZADO, passeio.Status);
    }

    [Fact]
    public void SaltosDeEstado_NaoSaoPermitidos()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();

        Assert.Throws<InvalidOperationException>(() => passeio.Iniciar(passeador));
        Assert.Throws<InvalidOperationException>(() => passeio.Finalizar(passeador));

        passeio.Aceitar(passeador);

        Assert.Throws<InvalidOperationException>(() => passeio.Finalizar(passeador));

        passeio.Iniciar(passeador);

        Assert.Throws<InvalidOperationException>(() => passeio.Iniciar(passeador));
        Assert.Equal(StatusPasseio.EM_ANDAMENTO, passeio.Status);
    }

    [Fact]
    public void UsuarioQueNaoEhOPasseador_NaoPodeIniciarNemFinalizar()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();
        passeio.Aceitar(passeador);
        var estranho = CriarUsuario(TipoPerfil.PASSEADOR);

        var erroIniciar = Assert.Throws<InvalidOperationException>(() => passeio.Iniciar(estranho));
        Assert.Equal("Passeio não pertence a este passeador", erroIniciar.Message);

        passeio.Iniciar(passeador);

        var erroFinalizar = Assert.Throws<InvalidOperationException>(() => passeio.Finalizar(estranho));
        Assert.Equal("Passeio não pertence a este passeador", erroFinalizar.Message);
        Assert.Equal(StatusPasseio.EM_ANDAMENTO, passeio.Status);
    }

    [Fact]
    public void PasseioFinalizado_NaoPermiteNovasTransicoes()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();
        passeio.Aceitar(passeador);
        passeio.Iniciar(passeador);
        passeio.Finalizar(passeador);

        Assert.Throws<InvalidOperationException>(() => passeio.Aceitar(passeador));
        Assert.Throws<InvalidOperationException>(() => passeio.Recusar(passeador));
        Assert.Throws<InvalidOperationException>(() => passeio.Iniciar(passeador));
        Assert.Throws<InvalidOperationException>(() => passeio.Finalizar(passeador));
        Assert.Equal(StatusPasseio.FINALIZADO, passeio.Status);
    }

    [Fact]
    public void TutorEPasseador_CancelamPasseioSolicitadoOuAceito()
    {
        var porTutorSolicitado = CriarPasseioSolicitado();
        porTutorSolicitado.passeio.Cancelar(porTutorSolicitado.passeio.Tutor);
        Assert.Equal(StatusPasseio.CANCELADO, porTutorSolicitado.passeio.Status);

        var porPasseadorSolicitado = CriarPasseioSolicitado();
        porPasseadorSolicitado.passeio.Cancelar(porPasseadorSolicitado.passeador);
        Assert.Equal(StatusPasseio.CANCELADO, porPasseadorSolicitado.passeio.Status);

        var porTutorAceito = CriarPasseioSolicitado();
        porTutorAceito.passeio.Aceitar(porTutorAceito.passeador);
        porTutorAceito.passeio.Cancelar(porTutorAceito.passeio.Tutor);
        Assert.Equal(StatusPasseio.CANCELADO, porTutorAceito.passeio.Status);

        var porPasseadorAceito = CriarPasseioSolicitado();
        porPasseadorAceito.passeio.Aceitar(porPasseadorAceito.passeador);
        porPasseadorAceito.passeio.Cancelar(porPasseadorAceito.passeador);
        Assert.Equal(StatusPasseio.CANCELADO, porPasseadorAceito.passeio.Status);
    }

    [Fact]
    public void UsuarioQueNaoParticipou_NaoPodeCancelar()
    {
        var (passeio, _) = CriarPasseioSolicitado();
        var estranho = CriarUsuario(TipoPerfil.PASSEADOR);

        var erro = Assert.Throws<InvalidOperationException>(() => passeio.Cancelar(estranho));

        Assert.Equal("Usuário não participou deste passeio.", erro.Message);
        Assert.Equal(StatusPasseio.SOLICITADO, passeio.Status);
    }

    [Fact]
    public void PasseioForaDeSolicitadoOuAceito_NaoPodeSerCancelado()
    {
        var (emAndamento, passeadorEmAndamento) = CriarPasseioSolicitado();
        emAndamento.Aceitar(passeadorEmAndamento);
        emAndamento.Iniciar(passeadorEmAndamento);

        var erroEmAndamento = Assert.Throws<InvalidOperationException>(() => emAndamento.Cancelar(passeadorEmAndamento));
        Assert.Equal("Passeio não pode ser cancelado no estado atual.", erroEmAndamento.Message);
        Assert.Equal(StatusPasseio.EM_ANDAMENTO, emAndamento.Status);

        var (finalizado, passeadorFinalizado) = CriarPasseioSolicitado();
        finalizado.Aceitar(passeadorFinalizado);
        finalizado.Iniciar(passeadorFinalizado);
        finalizado.Finalizar(passeadorFinalizado);

        var erroFinalizado = Assert.Throws<InvalidOperationException>(() => finalizado.Cancelar(passeadorFinalizado));
        Assert.Equal("Passeio não pode ser cancelado no estado atual.", erroFinalizado.Message);
        Assert.Equal(StatusPasseio.FINALIZADO, finalizado.Status);

        var (recusado, passeadorRecusado) = CriarPasseioSolicitado();
        recusado.Recusar(passeadorRecusado);

        var erroRecusado = Assert.Throws<InvalidOperationException>(() => recusado.Cancelar(passeadorRecusado));
        Assert.Equal("Passeio não pode ser cancelado no estado atual.", erroRecusado.Message);
        Assert.Equal(StatusPasseio.RECUSADO, recusado.Status);

        var (cancelado, passeadorCancelado) = CriarPasseioSolicitado();
        cancelado.Cancelar(passeadorCancelado);

        var erroCancelado = Assert.Throws<InvalidOperationException>(() => cancelado.Cancelar(passeadorCancelado));
        Assert.Equal("Passeio não pode ser cancelado no estado atual.", erroCancelado.Message);
        Assert.Equal(StatusPasseio.CANCELADO, cancelado.Status);
    }

    [Fact]
    public void PasseioSoParticipaQuemEhTutorOuPasseador()
    {
        var (passeio, passeador) = CriarPasseioSolicitado();
        var estranho = CriarUsuario(TipoPerfil.PASSEADOR);

        Assert.True(passeio.Participou(passeio.Tutor));
        Assert.True(passeio.Participou(passeador));
        Assert.False(passeio.Participou(estranho));
        Assert.False(passeio.Participou(null!));
    }

    private static (Passeio passeio, Usuario passeador) CriarPasseioSolicitado()
    {
        var (_, passeador, _, passeio) = Fabrica.CriarPasseioSolicitado();

        return (passeio, passeador);
    }
}
