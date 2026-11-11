using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Contrato do INotificacaoRepositorio com persistência real: SQLite em memória, navegações
// carregadas explicitamente e a Unidade de Trabalho como único ponto de SaveChanges.
public class NotificacaoRepositorioEfTests
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

    // Semeia tutor, pet, passeador e o passeio persistido ao qual a notificação se refere.
    private static (Usuario tutor, Usuario passeador, Pet pet, Passeio passeio) SemearPasseio(
        PetWalkDbContext contexto)
    {
        var (tutor, passeador, pet, passeio) = Fabrica.CriarPasseioSolicitado();

        contexto.AddRange(tutor, pet, passeador);
        contexto.SaveChanges();

        contexto.Add(passeio);
        contexto.SaveChanges();

        return (tutor, passeador, pet, passeio);
    }

    [Fact]
    public void Adicionar_PersisteNotificacaoEIdNasceAoSalvar()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        var (tutor, _, _, passeio) = SemearPasseio(contexto);

        var repositorio = new NotificacaoRepositorio(contexto);
        var unidade = new UnidadeDeTrabalho(contexto);

        var notificacao = repositorio.Adicionar(
            new Notificacao(tutor, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora));

        // O repositório não salva: sem a Unidade de Trabalho não existe Id.
        Assert.Equal(0, notificacao.Id);

        unidade.Salvar();

        Assert.NotEqual(0, notificacao.Id);

        using var leitura = CriarContexto(conexao);
        var recarregado = leitura.Notificacoes.Single(n => n.Id == notificacao.Id);

        Assert.Equal(TipoNotificacao.PASSEIO_SOLICITADO, recarregado.Tipo);
        Assert.False(recarregado.Lida);
        Assert.Equal(Fabrica.Agora, recarregado.DataEnvio);
    }

    [Fact]
    public void ObterPorId_CarregaDestinatarioEPasseio()
    {
        using var conexao = AbrirConexao();

        int notificacaoId;
        int tutorId;
        int passeioId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var (tutor, _, _, passeio) = SemearPasseio(escrita);

            var notificacao = new NotificacaoRepositorio(escrita)
                .Adicionar(new Notificacao(tutor, passeio, TipoNotificacao.PASSEIO_ACEITO, Fabrica.Agora));
            escrita.SaveChanges();

            notificacaoId = notificacao.Id;
            tutorId = tutor.Id;
            passeioId = passeio.Id;
        }

        using var leitura = CriarContexto(conexao);
        var carregada = new NotificacaoRepositorio(leitura).ObterPorId(notificacaoId);

        Assert.NotNull(carregada);
        Assert.Equal(tutorId, carregada!.Destinatario.Id);
        Assert.Equal(passeioId, carregada.Passeio.Id);

        // A mensagem persistida é a do envio, não uma nova execução de GerarMensagem.
        Assert.Equal("Seu passeio foi aceito.", carregada.Mensagem);
    }

    [Fact]
    public void ObterPorId_Inexistente_DevolveNulo()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        Assert.Null(new NotificacaoRepositorio(contexto).ObterPorId(99));
    }

    [Fact]
    public void ListarPorDestinatario_DevolveSomenteAsDoDestinatario_InclusiveAsLidas()
    {
        using var conexao = AbrirConexao();

        int tutorId;
        int passeadorId;
        int terceiroId;
        int passeioId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var (tutor, passeador, _, passeio) = SemearPasseio(escrita);
            var terceiro = Fabrica.CriarUsuario(TipoPerfil.TUTOR, "terceiro@petwalk.com");

            escrita.Add(terceiro);

            var repositorioDeEscrita = new NotificacaoRepositorio(escrita);

            var lida = repositorioDeEscrita.Adicionar(
                new Notificacao(tutor, passeio, TipoNotificacao.PASSEIO_ACEITO, Fabrica.Agora));
            lida.MarcarComoLida();

            repositorioDeEscrita.Adicionar(
                new Notificacao(tutor, passeio, TipoNotificacao.PASSEIO_INICIADO, Fabrica.Agora));
            repositorioDeEscrita.Adicionar(
                new Notificacao(passeador, passeio, TipoNotificacao.PASSEIO_SOLICITADO, Fabrica.Agora));

            escrita.SaveChanges();

            tutorId = tutor.Id;
            passeadorId = passeador.Id;
            terceiroId = terceiro.Id;
            passeioId = passeio.Id;
        }

        using var leitura = CriarContexto(conexao);
        var repositorio = new NotificacaoRepositorio(leitura);

        var doTutor = repositorio.ListarPorDestinatario(tutorId);
        var doPasseador = repositorio.ListarPorDestinatario(passeadorId);

        // Lida não é filtrada pelo repositório: apenasNaoLidas é decisão do serviço.
        Assert.Equal(2, doTutor.Count);
        Assert.Contains(doTutor, n => n.Lida);
        Assert.Contains(doTutor, n => !n.Lida);

        // As navegações lidas por NotificacaoResponse e pelo MarcarComoLida estão carregadas.
        Assert.All(doTutor, n => Assert.Equal(tutorId, n.Destinatario.Id));
        Assert.All(doTutor, n => Assert.Equal(passeioId, n.Passeio.Id));

        var doPasseadorUnica = Assert.Single(doPasseador);
        Assert.Equal(TipoNotificacao.PASSEIO_SOLICITADO, doPasseadorUnica.Tipo);

        Assert.Empty(repositorio.ListarPorDestinatario(terceiroId));
    }

    [Fact]
    public void NotificacaoCarregadaContinuaRastreada_MarcacaoComoLidaPersiste()
    {
        using var conexao = AbrirConexao();

        int notificacaoId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var (tutor, _, _, passeio) = SemearPasseio(escrita);

            var notificacao = new NotificacaoRepositorio(escrita)
                .Adicionar(new Notificacao(tutor, passeio, TipoNotificacao.PASSEIO_FINALIZADO, Fabrica.Agora));
            escrita.SaveChanges();

            notificacaoId = notificacao.Id;
        }

        using (var escrita = CriarContexto(conexao))
        {
            var repositorio = new NotificacaoRepositorio(escrita);
            var unidade = new UnidadeDeTrabalho(escrita);

            var notificacao = repositorio.ObterPorId(notificacaoId)!;

            Assert.Equal(EntityState.Unchanged, escrita.Entry(notificacao).State);

            // Mudança feita pelo domínio sobre a entidade carregada.
            notificacao.MarcarComoLida();

            escrita.ChangeTracker.DetectChanges();
            Assert.Equal(EntityState.Modified, escrita.Entry(notificacao).State);

            unidade.Salvar();
        }

        using var leitura = CriarContexto(conexao);
        Assert.True(leitura.Notificacoes.Single(n => n.Id == notificacaoId).Lida);
    }
}
