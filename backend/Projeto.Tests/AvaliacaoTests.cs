// Cobre a REG-001 e a REG-006: avaliação só após FINALIZADO, uma vez por avaliador,
// nota entre 1 e 5, com recálculo da média do avaliado.
public class AvaliacaoTests
{
    private static Usuario CriarUsuario(TipoPerfil tipoPerfil, string nome, string email) =>
        new(nome, email, "11999999999", "Rua A, 10", tipoPerfil, "hash-irrelevante");

    private static Pet CriarPet(Usuario tutor) =>
        new(tutor, "Rex", "Cachorro", "Vira-lata", Porte.MEDIO, 3, "Muito dócil");

    private static (Passeio passeio, Usuario tutor, Usuario passeador) CriarPasseioSolicitado()
    {
        var tutor = CriarUsuario(TipoPerfil.TUTOR, "Ana", "ana@petwalk.com");
        var passeador = CriarUsuario(TipoPerfil.PASSEADOR, "Bia", "bia@petwalk.com");
        var pet = CriarPet(tutor);
        tutor.AdicionarPet(pet);

        var passeio = tutor.SolicitarPasseio(pet, passeador, DateTime.UtcNow.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10");
        return (passeio, tutor, passeador);
    }

    private static (Passeio passeio, Usuario tutor, Usuario passeador) CriarPasseioFinalizado()
    {
        var (passeio, tutor, passeador) = CriarPasseioSolicitado();
        passeio.Aceitar(passeador);
        passeio.Iniciar(passeador);
        passeio.Finalizar(passeador);
        return (passeio, tutor, passeador);
    }

    [Fact]
    public void TutorAvaliaPasseador_RegistraAvaliacaoERecalculaMedia()
    {
        var (passeio, tutor, passeador) = CriarPasseioFinalizado();

        passeio.Avaliar(tutor, 5, "Ótimo passeio");

        var avaliacao = Assert.Single(passeio.Avaliacoes);
        Assert.Same(tutor, avaliacao.Avaliador);
        Assert.Same(passeador, avaliacao.Avaliado);
        Assert.Same(passeio, avaliacao.Passeio);
        Assert.Equal(5, avaliacao.Nota);
        Assert.Equal("Ótimo passeio", avaliacao.Comentario);
        Assert.Equal(5d, passeador.MediaAvaliacao);
        Assert.Equal(0d, tutor.MediaAvaliacao);
    }

    [Fact]
    public void PasseadorAvaliaTutor_RegistraAvaliacaoERecalculaMedia()
    {
        var (passeio, tutor, passeador) = CriarPasseioFinalizado();

        passeio.Avaliar(passeador, 4, "Tutor atencioso");

        var avaliacao = Assert.Single(passeio.Avaliacoes);
        Assert.Same(tutor, avaliacao.Avaliado);
        Assert.Equal(4d, tutor.MediaAvaliacao);
        Assert.Equal(0d, passeador.MediaAvaliacao);
    }

    [Fact]
    public void MesmoAvaliadorNaoPodeAvaliarDuasVezes()
    {
        var (passeio, tutor, _) = CriarPasseioFinalizado();
        passeio.Avaliar(tutor, 5, "Primeira avaliação");

        var erro = Assert.Throws<InvalidOperationException>(() => passeio.Avaliar(tutor, 3, "Segunda avaliação"));

        Assert.Equal("Passeio já avaliado por este usuário", erro.Message);
        Assert.Single(passeio.Avaliacoes);
        Assert.Equal(5d, passeio.Passeador.MediaAvaliacao);
    }

    [Fact]
    public void PasseioNaoFinalizado_NaoPodeSerAvaliado()
    {
        var (passeio, tutor, _) = CriarPasseioSolicitado();

        var erro = Assert.Throws<InvalidOperationException>(() => passeio.Avaliar(tutor, 5, ""));

        Assert.Equal("Passeio não finalizado", erro.Message);
        Assert.Empty(passeio.Avaliacoes);
    }

    [Fact]
    public void UsuarioQueNaoParticipou_NaoPodeAvaliar()
    {
        var (passeio, _, _) = CriarPasseioFinalizado();
        var estranho = CriarUsuario(TipoPerfil.AMBOS, "Carla", "carla@petwalk.com");

        var erro = Assert.Throws<InvalidOperationException>(() => passeio.Avaliar(estranho, 5, ""));

        Assert.Equal("Usuário não participou deste passeio", erro.Message);
        Assert.Empty(passeio.Avaliacoes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void NotaForaDoIntervalo_NaoPodeSerAvaliada(int nota)
    {
        var (passeio, tutor, _) = CriarPasseioFinalizado();

        var erro = Assert.Throws<ArgumentException>(() => passeio.Avaliar(tutor, nota, ""));

        Assert.Equal("Nota deve estar entre 1 e 5", erro.Message);
        Assert.Empty(passeio.Avaliacoes);
    }

    [Fact]
    public void AmbosAvaliam_AsMediasSaoIndividuais()
    {
        var (passeio, tutor, passeador) = CriarPasseioFinalizado();

        passeio.Avaliar(tutor, 5, "");
        passeio.Avaliar(passeador, 3, "");

        Assert.Equal(2, passeio.Avaliacoes.Count);
        Assert.Equal(5d, passeador.MediaAvaliacao);
        Assert.Equal(3d, tutor.MediaAvaliacao);
    }

    [Fact]
    public void AvaliacoesSaoSomenteLeitura()
    {
        var (passeio, _, _) = CriarPasseioFinalizado();
        var propriedade = typeof(Passeio).GetProperty(nameof(Passeio.Avaliacoes));

        Assert.NotNull(propriedade);
        Assert.Null(propriedade!.GetSetMethod());
        Assert.Equal(typeof(IReadOnlyList<Avaliacao>), propriedade.PropertyType);
        Assert.Empty(passeio.Avaliacoes);
    }
}
