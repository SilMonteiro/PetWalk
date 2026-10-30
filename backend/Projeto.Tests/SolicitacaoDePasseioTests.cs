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

        var passeio = tutor.SolicitarPasseio(pet, passeador, DateTime.UtcNow.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10");

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
            () => passeador.SolicitarPasseio(pet, passeador, DateTime.UtcNow.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10"));

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
            () => tutor.SolicitarPasseio(petDoOutro, passeador, DateTime.UtcNow.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10"));

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
            () => tutor.SolicitarPasseio(pet, passeador, DateTime.UtcNow.AddDays(-1), TimeSpan.FromHours(1), "Rua A, 10"));

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
            () => tutor.SolicitarPasseio(pet, outroTutor, DateTime.UtcNow.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10"));

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

        Assert.Equal("O passeio não está em estado de solicitação.", erro.Message);
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
        var tutor = CriarUsuario(TipoPerfil.TUTOR);
        var passeador = CriarUsuario(TipoPerfil.PASSEADOR);
        var pet = CriarPet(tutor);
        tutor.AdicionarPet(pet);

        var passeio = tutor.SolicitarPasseio(pet, passeador, DateTime.UtcNow.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10");

        return (passeio, passeador);
    }
}
