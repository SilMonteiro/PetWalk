using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// 8.6: as duas corridas restantes disputam um índice único. A violação é real do SQLite: os
// dois contextos carregam o estado antes da primeira gravação, então quem barra o perdedor é a
// constraint do banco — exatamente o caminho que ErroDePersistencia precisa traduzir.
public class UnicidadeSobConcorrenciaTests
{
    private static SqliteConnection AbrirConexao()
    {
        var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();

        return conexao;
    }

    private static PetWalkDbContext CriarContexto(SqliteConnection conexao) =>
        new(new DbContextOptionsBuilder<PetWalkDbContext>().UseSqlite(conexao).Options);

    // Uma única conexão dona do banco: todos os contextos do teste enxergam o mesmo schema.
    private static void CriarSchema(SqliteConnection conexao)
    {
        using var contexto = CriarContexto(conexao);
        contexto.Database.EnsureCreated();
    }

    private static int StatusDe(IResult resultado) =>
        Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode!.Value;

    private static string? MensagemDe(IResult resultado)
    {
        var corpo = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;

        return corpo?.GetType().GetProperty("erro")?.GetValue(corpo) as string;
    }

    [Fact]
    public void EmailDuplicadoSobCorrida_ViolacaoRealMapeiaPara409()
    {
        const string email = "corrida@petwalk.com";

        using var conexao = AbrirConexao();
        CriarSchema(conexao);

        using var contextoA = CriarContexto(conexao);
        using var contextoB = CriarContexto(conexao);

        var servicoA = new AutenticacaoServico(new UsuarioRepositorio(contextoA));
        var servicoB = new AutenticacaoServico(new UsuarioRepositorio(contextoB));

        // B executa o check-then-insert antes de A gravar: os dois passam pelo pre-check de
        // e-mail e só a constraint do índice único pode barrar o segundo.
        servicoB.Cadastrar("Corrida B", email, "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "123456");
        servicoA.Cadastrar("Corrida A", email, "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "123456");

        new UnidadeDeTrabalho(contextoA).Salvar();

        var erro = Assert.Throws<DbUpdateException>(() => new UnidadeDeTrabalho(contextoB).Salvar());

        var resultado = ErroDePersistencia.Mapear(erro);

        Assert.NotNull(resultado);
        Assert.Equal(409, StatusDe(resultado!));
        Assert.Equal("E-mail já cadastrado", MensagemDe(resultado!));

        // A gravação do perdedor foi revertida: existe um único usuário com esse e-mail.
        using var leitura = CriarContexto(conexao);
        Assert.Equal(1, leitura.Usuarios.Count(u => u.Email == email));
    }

    [Fact]
    public void AvaliacaoDuplicadaSobCorrida_ViolacaoRealMapeiaPara422()
    {
        using var conexao = AbrirConexao();
        CriarSchema(conexao);

        var (tutor, passeador, pet, passeio) = Fabrica.CriarPasseioSolicitado();

        using (var escrita = CriarContexto(conexao))
        {
            escrita.AddRange(tutor, pet, passeador);
            escrita.SaveChanges();

            new PasseioRepositorio(escrita).Adicionar(passeio);
            escrita.SaveChanges();
        }

        // FINALIZADO é pré-requisito da Avaliacao; a transição usa o passeador rastreado.
        using (var andamento = CriarContexto(conexao))
        {
            var paraFinalizar = new PasseioRepositorio(andamento).ObterPorId(passeio.Id)!;

            paraFinalizar.Aceitar(paraFinalizar.Passeador);
            paraFinalizar.Iniciar(paraFinalizar.Passeador);
            paraFinalizar.Finalizar(paraFinalizar.Passeador);

            new UnidadeDeTrabalho(andamento).Salvar();
        }

        using var contextoA = CriarContexto(conexao);
        using var contextoB = CriarContexto(conexao);

        // Os dois contextos carregam o passeio e o histórico (vazio) antes de qualquer gravação.
        var passeioA = new PasseioRepositorio(contextoA).ObterParaAvaliacao(passeio.Id)!;
        var passeioB = new PasseioRepositorio(contextoB).ObterParaAvaliacao(passeio.Id)!;

        Assert.Empty(passeioA.Avaliacoes);
        Assert.Empty(passeioB.Avaliacoes);

        // O mesmo avaliador nos dois lados: o índice único é (PasseioId, AvaliadorId).
        passeioA.Avaliar(passeioA.Tutor, 5, "Muito atencioso", Fabrica.Agora);
        passeioB.Avaliar(passeioB.Tutor, 3, "Atrasou", Fabrica.Agora);

        new UnidadeDeTrabalho(contextoA).Salvar();

        var erro = Assert.Throws<DbUpdateException>(() => new UnidadeDeTrabalho(contextoB).Salvar());

        var resultado = ErroDePersistencia.Mapear(erro);

        Assert.NotNull(resultado);
        Assert.Equal(422, StatusDe(resultado!));
        Assert.Equal("Passeio já avaliado por este usuário", MensagemDe(resultado!));

        // Sobrou só a avaliação vencedora (nota 5) e a média é a dela — a do perdedor (nota 3)
        // foi revertida junto com o INSERT que violou o índice.
        using var leitura = CriarContexto(conexao);
        var unica = Assert.Single(leitura.Avaliacoes);

        Assert.Equal(5, unica.Nota);
        Assert.Equal(5d, leitura.Usuarios.Single(u => u.Id == passeador.Id).MediaAvaliacao);
    }
}
