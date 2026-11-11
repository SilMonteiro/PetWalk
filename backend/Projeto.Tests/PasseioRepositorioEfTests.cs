using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Contrato do IPasseioRepositorio com persistência real: SQLite em memória, navegações
// carregadas explicitamente e a Unidade de Trabalho como único ponto de SaveChanges.
public class PasseioRepositorioEfTests
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

    // Semeia tutor, pet e passeador e devolve o passeio ainda não salvo.
    private static (Usuario tutor, Usuario passeador, Pet pet, Passeio passeio) SemearParticipantes(
        PetWalkDbContext contexto)
    {
        var (tutor, passeador, pet, passeio) = Fabrica.CriarPasseioSolicitado(valorHora: 50m);

        contexto.AddRange(tutor, pet, passeador);
        contexto.SaveChanges();

        return (tutor, passeador, pet, passeio);
    }

    [Fact]
    public void Adicionar_PersistePasseioEIdNasceAoSalvar()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        var (_, _, _, passeio) = SemearParticipantes(contexto);

        var repositorio = new PasseioRepositorio(contexto);
        var unidade = new UnidadeDeTrabalho(contexto);

        repositorio.Adicionar(passeio);

        // O repositório não salva: sem a Unidade de Trabalho não existe Id.
        Assert.Equal(0, passeio.Id);

        unidade.Salvar();

        Assert.NotEqual(0, passeio.Id);

        using var leitura = CriarContexto(conexao);
        var recarregado = leitura.Passeios.Single(p => p.Id == passeio.Id);

        Assert.Equal(StatusPasseio.SOLICITADO, recarregado.Status);
        Assert.Equal(TimeSpan.FromHours(1), recarregado.Duracao);
        Assert.Equal(50m, recarregado.Valor);
    }

    [Fact]
    public void ObterPorId_Inexistente_DevolveNulo()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        Assert.Null(new PasseioRepositorio(contexto).ObterPorId(99));
    }

    [Fact]
    public void ObterPorId_CarregaParticipantesPetEAvaliacoesComAvaliador()
    {
        using var conexao = AbrirConexao();

        int passeioId;
        int tutorId;
        int passeadorId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var (tutor, passeador, _, passeio) = SemearParticipantes(escrita);

            new PasseioRepositorio(escrita).Adicionar(passeio);
            new UnidadeDeTrabalho(escrita).Salvar();

            passeioId = passeio.Id;
            tutorId = tutor.Id;
            passeadorId = passeador.Id;
        }

        using (var escrita = CriarContexto(conexao))
        {
            var passeio = new PasseioRepositorio(escrita).ObterPorId(passeioId)!;

            // Transições e avaliação sobre as entidades que o repositório carregou:
            // tutor e passeador são as instâncias rastreadas neste mesmo contexto.
            passeio.Aceitar(passeio.Passeador);
            passeio.Iniciar(passeio.Passeador);
            passeio.Finalizar(passeio.Passeador);
            passeio.Avaliar(passeio.Tutor, 5, "Excelente", Fabrica.Agora);

            new UnidadeDeTrabalho(escrita).Salvar();
        }

        using var leitura = CriarContexto(conexao);
        var carregado = new PasseioRepositorio(leitura).ObterPorId(passeioId);

        Assert.NotNull(carregado);
        Assert.Equal(StatusPasseio.FINALIZADO, carregado!.Status);
        Assert.Equal(tutorId, carregado.Tutor.Id);
        Assert.Equal(passeadorId, carregado.Passeador.Id);
        Assert.Equal("Rex", carregado.Pet.Nome);

        var avaliacao = Assert.Single(carregado.Avaliacoes);
        Assert.Equal(5, avaliacao.Nota);
        Assert.Equal(tutorId, avaliacao.Avaliador.Id);
        Assert.Equal(passeadorId, avaliacao.Avaliado.Id);

        // RegistrarAvaliacaoRecebida atualizou a média do avaliado e ela foi persistida.
        Assert.Equal(5d, carregado.Passeador.MediaAvaliacao);
    }

    [Fact]
    public void ListarPorUsuario_DevolveParaTutorEPasseador_MasNaoParaTerceiro()
    {
        using var conexao = AbrirConexao();

        int tutorId;
        int passeadorId;
        int terceiroId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var (tutor, passeador, _, passeio) = SemearParticipantes(escrita);
            var terceiro = Fabrica.CriarUsuario(TipoPerfil.TUTOR, "terceiro@petwalk.com");

            escrita.Add(terceiro);
            new PasseioRepositorio(escrita).Adicionar(passeio);
            new UnidadeDeTrabalho(escrita).Salvar();

            tutorId = tutor.Id;
            passeadorId = passeador.Id;
            terceiroId = terceiro.Id;
        }

        using var leitura = CriarContexto(conexao);
        var repositorio = new PasseioRepositorio(leitura);

        var doTutor = Assert.Single(repositorio.ListarPorUsuario(tutorId));
        var doPasseador = Assert.Single(repositorio.ListarPorUsuario(passeadorId));

        Assert.Empty(repositorio.ListarPorUsuario(terceiroId));

        // As navegações lidas por PasseioResponse estão carregadas.
        Assert.Equal("Rex", doTutor.Pet.Nome);
        Assert.Equal(tutorId, doTutor.Tutor.Id);
        Assert.Equal(passeadorId, doTutor.Passeador.Id);
        Assert.Equal(doTutor.Id, doPasseador.Id);
    }

    [Fact]
    public void ExisteParaPet_VerdadeiroQuandoOPetTemPasseio()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        var (_, _, pet, passeio) = SemearParticipantes(contexto);

        var repositorio = new PasseioRepositorio(contexto);
        repositorio.Adicionar(passeio);
        new UnidadeDeTrabalho(contexto).Salvar();

        Assert.True(repositorio.ExisteParaPet(pet.Id));
    }

    [Fact]
    public void ExisteParaPet_FalsoQuandoOPetNaoTemPasseio()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
        var pet = Fabrica.CriarPet(tutor);
        tutor.AdicionarPet(pet);

        contexto.AddRange(tutor, pet);
        contexto.SaveChanges();

        Assert.False(new PasseioRepositorio(contexto).ExisteParaPet(pet.Id));
    }

    [Fact]
    public void AlteracaoEmPasseioCarregadoPersistePelaUnidadeDeTrabalho()
    {
        using var conexao = AbrirConexao();

        int passeioId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var (_, _, _, passeio) = SemearParticipantes(escrita);

            new PasseioRepositorio(escrita).Adicionar(passeio);
            new UnidadeDeTrabalho(escrita).Salvar();

            passeioId = passeio.Id;
        }

        using (var escrita = CriarContexto(conexao))
        {
            var repositorio = new PasseioRepositorio(escrita);
            var unidade = new UnidadeDeTrabalho(escrita);

            var passeio = repositorio.ObterPorId(passeioId)!;

            Assert.Equal(EntityState.Unchanged, escrita.Entry(passeio).State);

            // Mudança feita pelo domínio sobre a entidade carregada.
            passeio.Aceitar(passeio.Passeador);

            escrita.ChangeTracker.DetectChanges();
            Assert.Equal(EntityState.Modified, escrita.Entry(passeio).State);

            unidade.Salvar();
        }

        using var leitura = CriarContexto(conexao);
        Assert.Equal(StatusPasseio.ACEITO, leitura.Passeios.Single(p => p.Id == passeioId).Status);
    }
}
