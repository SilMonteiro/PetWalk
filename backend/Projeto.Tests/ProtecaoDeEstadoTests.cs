using System.Reflection;

// Proteção de estado: as entidades só mudam pelos próprios métodos, nunca por setter público.
public class ProtecaoDeEstadoTests
{
    private static Usuario CriarTutor() =>
        new("Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "hash");

    private static Usuario CriarPasseador() =>
        new("Bia", "bia@petwalk.com", "11888888888", "Rua B, 20", TipoPerfil.PASSEADOR, "hash");

    private static Pet CriarPet(Usuario tutor) =>
        new(tutor, "Rex", "Cachorro", "Vira-lata", Porte.MEDIO, 3, "Muito dócil");

    private static (Usuario tutor, Usuario passeador, Pet pet, Passeio passeio) CriarPasseioSolicitado()
    {
        var tutor = CriarTutor();
        var passeador = CriarPasseador();
        var pet = CriarPet(tutor);
        tutor.AdicionarPet(pet);

        var passeio = tutor.SolicitarPasseio(pet, passeador, DateTime.UtcNow.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10");

        return (tutor, passeador, pet, passeio);
    }

    // ---------- Pet ----------

    [Fact]
    public void Pet_ComNomeVazio_LancaErro()
    {
        var tutor = CriarTutor();

        var erro = Assert.Throws<ArgumentException>(
            () => new Pet(tutor, "  ", "Cachorro", "Vira-lata", Porte.MEDIO, 3, ""));

        Assert.Equal("Nome do pet é obrigatório.", erro.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void Pet_ComIdadeForaDoIntervalo_LancaErro(int idade)
    {
        var tutor = CriarTutor();

        var erro = Assert.Throws<ArgumentException>(
            () => new Pet(tutor, "Rex", "Cachorro", "Vira-lata", Porte.MEDIO, idade, ""));

        Assert.Equal("Idade inválida.", erro.Message);
    }

    [Fact]
    public void Pet_SemTutor_LancaErro()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Pet(null!, "Rex", "Cachorro", "Vira-lata", Porte.MEDIO, 3, ""));
    }

    [Fact]
    public void Pet_VacinasSaoSomenteLeitura()
    {
        var pet = CriarPet(CriarTutor());

        pet.RegistrarVacina("V8");
        pet.RegistrarVacina("Antirrábica");

        var propriedade = typeof(Pet).GetProperty(nameof(Pet.Vacinas));

        Assert.NotNull(propriedade);
        Assert.Null(propriedade!.GetSetMethod());
        Assert.Equal(typeof(IReadOnlyList<string>), propriedade.PropertyType);
        Assert.Equal(new[] { "V8", "Antirrábica" }, pet.Vacinas);
    }

    [Fact]
    public void Pet_EstadoNaoMudaPorSetterPublico()
    {
        foreach (var nome in new[] { nameof(Pet.Nome), nameof(Pet.Especie), nameof(Pet.Raca), nameof(Pet.Porte), nameof(Pet.Idade), nameof(Pet.Observacoes), nameof(Pet.Tutor), nameof(Pet.Id) })
        {
            AssertSemSetterPublico(typeof(Pet), nome);
        }
    }

    // ---------- Usuario ----------

    [Fact]
    public void Usuario_ComDadosValidos_CriaComEstadoInicial()
    {
        var usuario = CriarTutor();

        Assert.Equal("Ana", usuario.Nome);
        Assert.Equal("ana@petwalk.com", usuario.Email);
        Assert.Equal(TipoPerfil.TUTOR, usuario.TipoPerfil);
        Assert.Equal(StatusUsuario.ATIVO, usuario.Status);
        Assert.Equal("hash", usuario.SenhaHash);
        Assert.True(usuario.EhTutor);
        Assert.False(usuario.EhPasseador);
    }

    [Theory]
    [InlineData("", "ana@petwalk.com", "hash")]
    [InlineData("Ana", "", "hash")]
    [InlineData("Ana", "ana@petwalk.com", "")]
    public void Usuario_SemDadoObrigatorio_LancaErro(string nome, string email, string senhaHash)
    {
        var erro = Assert.Throws<ArgumentException>(
            () => new Usuario(nome, email, "11999999999", "Rua A, 10", TipoPerfil.TUTOR, senhaHash));

        Assert.Equal("Nome, e-mail e senha são obrigatórios.", erro.Message);
    }

    [Fact]
    public void Usuario_PetsSaoSomenteLeitura()
    {
        var tutor = CriarTutor();

        var propriedade = typeof(Usuario).GetProperty(nameof(Usuario.Pets));

        Assert.NotNull(propriedade);
        Assert.Null(propriedade!.GetSetMethod());
        Assert.Equal(typeof(IReadOnlyList<Pet>), propriedade.PropertyType);
        Assert.Empty(tutor.Pets);

        tutor.AdicionarPet(CriarPet(tutor));

        Assert.Single(tutor.Pets);
    }

    [Fact]
    public void Usuario_EnderecoNaoVazaParaOutroUsuario()
    {
        var tutor = CriarTutor();
        var outro = CriarPasseador();

        Assert.Equal("Rua A, 10", tutor.ObterEnderecoPara(tutor));

        var erro = Assert.Throws<InvalidOperationException>(() => tutor.ObterEnderecoPara(outro));
        Assert.Equal("Acesso não autorizado ao endereço do perfil", erro.Message);
    }

    // ---------- Passeio ----------

    [Fact]
    public void Passeio_CriadoPelaSolicitacao_JaNasceSolicitado()
    {
        var (tutor, passeador, pet, passeio) = CriarPasseioSolicitado();

        Assert.Equal(StatusPasseio.SOLICITADO, passeio.Status);
        Assert.Same(tutor, passeio.Tutor);
        Assert.Same(passeador, passeio.Passeador);
        Assert.Same(pet, passeio.Pet);
        Assert.Equal("Rua A, 10", passeio.Local);
    }

    [Fact]
    public void Passeio_Participou_ReconheceApenasTutorEPasseador()
    {
        var (tutor, passeador, _, passeio) = CriarPasseioSolicitado();
        var estranho = new Usuario("Carla", "carla@petwalk.com", "11777777777", "Rua C, 30", TipoPerfil.AMBOS, "hash");

        Assert.True(passeio.Participou(tutor));
        Assert.True(passeio.Participou(passeador));
        Assert.False(passeio.Participou(estranho));
        Assert.False(passeio.Participou(null!));
    }

    [Fact]
    public void Passeio_EstadoNaoMudaPorSetterPublico()
    {
        foreach (var nome in new[] { nameof(Passeio.Id), nameof(Passeio.DataHora), nameof(Passeio.Duracao), nameof(Passeio.Local), nameof(Passeio.Status), nameof(Passeio.Tutor), nameof(Passeio.Passeador), nameof(Passeio.Pet) })
        {
            AssertSemSetterPublico(typeof(Passeio), nome);
        }
    }

    // ---------- Notificacao ----------

    [Fact]
    public void Notificacao_LidaComecaFalsa()
    {
        var (tutor, _, _, passeio) = CriarPasseioSolicitado();

        var notificacao = new Notificacao(tutor, passeio, TipoNotificacao.PASSEIO_ACEITO);

        Assert.False(notificacao.Lida);
        Assert.Equal(TipoNotificacao.PASSEIO_ACEITO, notificacao.Tipo);
        Assert.False(string.IsNullOrWhiteSpace(notificacao.Mensagem));
        Assert.Same(tutor, notificacao.Destinatario);
    }

    [Fact]
    public void Notificacao_MarcarComoLida_TornaLida()
    {
        var (tutor, _, _, passeio) = CriarPasseioSolicitado();
        var notificacao = new Notificacao(tutor, passeio, TipoNotificacao.PASSEIO_FINALIZADO);

        notificacao.MarcarComoLida();

        Assert.True(notificacao.Lida);
    }

    [Fact]
    public void Notificacao_LidaNaoMudaPorSetterPublico()
    {
        foreach (var nome in new[] { nameof(Notificacao.Id), nameof(Notificacao.Lida), nameof(Notificacao.Mensagem), nameof(Notificacao.Tipo), nameof(Notificacao.DataEnvio), nameof(Notificacao.Destinatario), nameof(Notificacao.PasseioId) })
        {
            AssertSemSetterPublico(typeof(Notificacao), nome);
        }
    }

    private static void AssertSemSetterPublico(Type tipo, string nomeDaPropriedade)
    {
        var propriedade = tipo.GetProperty(nomeDaPropriedade, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(propriedade);
        Assert.Null(propriedade!.GetSetMethod());
    }
}
