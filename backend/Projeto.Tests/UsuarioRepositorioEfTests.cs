using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Contrato do IUsuarioRepositorio com persistência real: SQLite em memória e a
// Unidade de Trabalho como único ponto de SaveChanges.
public class UsuarioRepositorioEfTests
{
    // SQLite em memória só existe enquanto a conexão estiver aberta: cada teste abre a sua.
    private static SqliteConnection AbrirConexao()
    {
        var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();

        return conexao;
    }

    private static PetWalkDbContext CriarContexto(SqliteConnection conexao) =>
        new(new DbContextOptionsBuilder<PetWalkDbContext>().UseSqlite(conexao).Options);

    // O schema é criado apenas no setup de cada teste.
    private static PetWalkDbContext CriarContextoComSchema(SqliteConnection conexao)
    {
        var contexto = CriarContexto(conexao);
        contexto.Database.EnsureCreated();

        return contexto;
    }

    [Fact]
    public void Adicionar_PersisteUsuarioEIdNasceAoSalvar()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        var repositorio = new UsuarioRepositorio(contexto);
        var unidade = new UnidadeDeTrabalho(contexto);

        var usuario = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR, "tutor@petwalk.com"));

        // O repositório não salva: sem a Unidade de Trabalho não existe Id.
        Assert.Equal(0, usuario.Id);

        unidade.Salvar();

        Assert.NotEqual(0, usuario.Id);

        using var leitura = CriarContexto(conexao);
        var recarregado = leitura.Usuarios.Single(u => u.Id == usuario.Id);

        Assert.Equal("tutor@petwalk.com", recarregado.Email);
        Assert.Equal(StatusUsuario.ATIVO, recarregado.Status);
    }

    [Fact]
    public void ObterPorId_IncluiOsPetsDoTutorSemMisturarAgregados()
    {
        using var conexao = AbrirConexao();

        int tutorId;
        int petId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR, "tutor@petwalk.com");
            var pet = Fabrica.CriarPet(tutor);
            tutor.AdicionarPet(pet);
            escrita.AddRange(tutor, pet);
            escrita.SaveChanges();

            tutorId = tutor.Id;
            petId = pet.Id;

            // Segundo agregado: a busca por Id não pode trazer pets de outro tutor.
            var outro = Fabrica.CriarUsuario(TipoPerfil.TUTOR, "outro@petwalk.com");
            var petDoOutro = Fabrica.CriarPet(outro);
            outro.AdicionarPet(petDoOutro);
            escrita.AddRange(outro, petDoOutro);
            escrita.SaveChanges();
        }

        using var leitura = CriarContexto(conexao);
        var repositorio = new UsuarioRepositorio(leitura);

        var usuario = repositorio.ObterPorId(tutorId);

        Assert.NotNull(usuario);

        var petDoTutor = Assert.Single(usuario!.Pets);
        Assert.Equal(petId, petDoTutor.Id);
        Assert.Equal("Rex", petDoTutor.Nome);
    }

    [Theory]
    [InlineData("PASSEADOR@PETWALK.COM")]
    [InlineData("Passeador@PetWalk.com")]
    public void ObterPorEmail_IgnoraMaiusculasEMinusculas(string email)
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        var repositorio = new UsuarioRepositorio(contexto);
        var unidade = new UnidadeDeTrabalho(contexto);

        var usuario = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "passeador@petwalk.com"));
        unidade.Salvar();

        Assert.NotNull(repositorio.ObterPorEmail(email));
        Assert.Equal(usuario.Id, repositorio.ObterPorEmail(email)!.Id);
    }

    [Fact]
    public void ObterPorEmail_Inexistente_DevolveNulo()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        var repositorio = new UsuarioRepositorio(contexto);

        Assert.Null(repositorio.ObterPorEmail("ninguem@petwalk.com"));
    }

    [Fact]
    public void ListarPasseadoresAtivos_DevolvePasseadorEAmbos_ExcluiTutorEBloqueado()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        var repositorio = new UsuarioRepositorio(contexto);
        var unidade = new UnidadeDeTrabalho(contexto);

        repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "passeador@petwalk.com"));
        repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.AMBOS, "ambos@petwalk.com"));
        repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.TUTOR, "tutor@petwalk.com"));

        var bloqueado = repositorio.Adicionar(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "bloqueado@petwalk.com"));
        bloqueado.Bloquear();

        unidade.Salvar();

        var ativos = repositorio.ListarPasseadoresAtivos();

        Assert.Equal(2, ativos.Count);
        Assert.Contains(ativos, u => u.Email == "passeador@petwalk.com");
        Assert.Contains(ativos, u => u.Email == "ambos@petwalk.com");
        Assert.DoesNotContain(ativos, u => u.Email == "tutor@petwalk.com");
        Assert.DoesNotContain(ativos, u => u.Email == "bloqueado@petwalk.com");
    }

    [Fact]
    public void RegistrarPet_PersistePetComIdGeradoEVinculaAoTutor()
    {
        using var conexao = AbrirConexao();

        int tutorId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR, "tutor@petwalk.com");
            escrita.Add(tutor);
            escrita.SaveChanges();

            tutorId = tutor.Id;
        }

        int petId;

        using (var escrita = CriarContexto(conexao))
        {
            var repositorio = new UsuarioRepositorio(escrita);
            var unidade = new UnidadeDeTrabalho(escrita);

            var tutor = repositorio.ObterPorId(tutorId)!;
            var pet = Fabrica.CriarPet(tutor);

            repositorio.RegistrarPet(tutor, pet);

            Assert.Equal(0, pet.Id);

            unidade.Salvar();

            Assert.NotEqual(0, pet.Id);
            petId = pet.Id;

            Assert.Contains(pet, tutor.Pets);
            Assert.Same(pet, repositorio.ObterPet(petId));
        }

        using var leitura = CriarContexto(conexao);
        var petRecarregado = leitura.Pets.Include(p => p.Tutor).Single(p => p.Id == petId);

        Assert.Equal("Rex", petRecarregado.Nome);
        Assert.Equal(tutorId, petRecarregado.Tutor.Id);
    }

    [Fact]
    public void ObterPet_Inexistente_DevolveNulo()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        var repositorio = new UsuarioRepositorio(contexto);

        Assert.Null(repositorio.ObterPet(99));
    }

    [Fact]
    public void EntidadeCarregadaContinuaRastreada_AlteracaoPersisteComUnidadeDeTrabalho()
    {
        using var conexao = AbrirConexao();

        int usuarioId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var usuario = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "passeador@petwalk.com");
            escrita.Add(usuario);
            escrita.SaveChanges();

            usuarioId = usuario.Id;
        }

        using (var escrita = CriarContexto(conexao))
        {
            var repositorio = new UsuarioRepositorio(escrita);
            var unidade = new UnidadeDeTrabalho(escrita);

            var usuario = repositorio.ObterPorId(usuarioId)!;

            Assert.Equal(EntityState.Unchanged, escrita.Entry(usuario).State);

            // Mudança feita pelo domínio sobre a entidade carregada.
            usuario.DefinirCobranca(50m, TipoCobranca.POR_HORA);

            escrita.ChangeTracker.DetectChanges();
            Assert.Equal(EntityState.Modified, escrita.Entry(usuario).State);

            unidade.Salvar();
        }

        using var leitura = CriarContexto(conexao);
        var recarregado = leitura.Usuarios.Single(u => u.Id == usuarioId);

        Assert.Equal(50m, recarregado.ValorHora);
        Assert.Equal(TipoCobranca.POR_HORA, recarregado.TipoCobranca);
    }
}
