using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Tradução de DbUpdateException em resposta HTTP: os testes provocam violações reais do
// SQLite e conferem o mapeamento por código estendido + entidade rastreada.
public class ErroDePersistenciaTests
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

    private static PetWalkDbContext CriarContextoComSchema(SqliteConnection conexao)
    {
        var contexto = CriarContexto(conexao);
        contexto.Database.EnsureCreated();

        return contexto;
    }

    [Fact]
    public void EmailDuplicado_MapeiaPara409()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        contexto.Add(Fabrica.CriarUsuario(TipoPerfil.TUTOR, "duplicado@petwalk.com"));
        contexto.SaveChanges();

        contexto.Add(Fabrica.CriarUsuario(TipoPerfil.PASSEADOR, "duplicado@petwalk.com"));
        var falha = Assert.Throws<DbUpdateException>(() => contexto.SaveChanges());

        var resposta = ErroDePersistencia.Mapear(falha);

        Assert.NotNull(resposta);
        Assert.Equal(409, StatusDe(resposta!));
        Assert.Equal("E-mail já cadastrado", MensagemDe(resposta!));
    }

    [Fact]
    public void AvaliacaoDuplicada_MapeiaPara422()
    {
        using var conexao = AbrirConexao();

        int passeioId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var (tutor, passeador, pet, passeio) = Fabrica.CriarPasseioSolicitado();

            escrita.AddRange(tutor, pet, passeador);
            escrita.SaveChanges();

            escrita.Add(passeio);
            escrita.SaveChanges();

            passeio.Aceitar(passeador);
            passeio.Iniciar(passeador);
            passeio.Finalizar(passeador);
            passeio.Avaliar(tutor, 5, "Bom", Fabrica.Agora);
            escrita.SaveChanges();

            passeioId = passeio.Id;
        }

        // Contexto novo carrega o passeio sem as avaliações (ObterPorId): o domínio não vê a
        // duplicata e quem precisa barrar é o índice único (PasseioId, AvaliadorId).
        using var segundo = CriarContexto(conexao);
        var passeioSemAvaliacoes = new PasseioRepositorio(segundo).ObterPorId(passeioId)!;

        passeioSemAvaliacoes.Avaliar(passeioSemAvaliacoes.Tutor, 3, "De novo", Fabrica.Agora);

        var falha = Assert.Throws<DbUpdateException>(() => segundo.SaveChanges());

        var resposta = ErroDePersistencia.Mapear(falha);

        Assert.NotNull(resposta);
        Assert.Equal(422, StatusDe(resposta!));
        Assert.Equal("Passeio já avaliado por este usuário", MensagemDe(resposta!));
    }

    [Fact]
    public void ChaveEstrangeiraAoExcluirUsuario_MapeiaPara409Generico()
    {
        using var conexao = AbrirConexao();

        int tutorId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
            var pet = Fabrica.CriarPet(tutor);
            tutor.AdicionarPet(pet);

            escrita.AddRange(tutor, pet);
            escrita.SaveChanges();

            tutorId = tutor.Id;
        }

        // O Pet não é carregado: assim o EF manda o DELETE e quem recusa é a FK do banco
        // (Pets.TutorId é Restrict). Com o Pet rastreado, o EF recusaria antes, em memória.
        using var contexto = CriarContexto(conexao);
        contexto.Usuarios.Remove(contexto.Usuarios.Single(u => u.Id == tutorId));

        var falha = Assert.Throws<DbUpdateException>(() => contexto.SaveChanges());

        var resposta = ErroDePersistencia.Mapear(falha);

        Assert.NotNull(resposta);
        Assert.Equal(409, StatusDe(resposta!));

        // Não é exclusão de Pet: a mensagem específica não se aplica.
        Assert.Equal("Operação não permitida: o registro está em uso", MensagemDe(resposta!));
    }

    [Fact]
    public void ChaveEstrangeiraAoExcluirPet_MapeiaPara409ComMensagemDePet()
    {
        using var conexao = AbrirConexao();

        int petId;

        using (var escrita = CriarContextoComSchema(conexao))
        {
            var (tutor, passeador, pet, passeio) = Fabrica.CriarPasseioSolicitado();

            escrita.AddRange(tutor, pet, passeador);
            escrita.SaveChanges();

            escrita.Add(passeio);
            escrita.SaveChanges();

            petId = pet.Id;
        }

        // Excluir o pet com passeio associado: Passeios.PetId é Restrict.
        using var contexto = CriarContexto(conexao);
        contexto.Pets.Remove(contexto.Pets.Single(p => p.Id == petId));

        var falha = Assert.Throws<DbUpdateException>(() => contexto.SaveChanges());

        var resposta = ErroDePersistencia.Mapear(falha);

        Assert.NotNull(resposta);
        Assert.Equal(409, StatusDe(resposta!));

        // A entidade Pet está sendo excluída: aqui a mensagem específica é segura.
        Assert.Equal("Pet possui passeios e não pode ser excluído", MensagemDe(resposta!));
    }

    [Fact]
    public void ChaveEstrangeiraAoInserirPet_MapeiaPara409Generico()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContextoComSchema(conexao);

        // Tutor não persistido: o Pet entra com TutorId 0, que não existe em Usuarios.
        var tutorFantasma = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
        var pet = Fabrica.CriarPet(tutorFantasma);

        contexto.Entry(pet).State = EntityState.Added;

        var falha = Assert.Throws<DbUpdateException>(() => contexto.SaveChanges());

        var resposta = ErroDePersistencia.Mapear(falha);

        Assert.NotNull(resposta);
        Assert.Equal(409, StatusDe(resposta!));

        // O Pet está sendo inserido, não excluído: mensagem genérica, não a de exclusão.
        Assert.Equal("Operação não permitida: o registro está em uso", MensagemDe(resposta!));
    }

    [Fact]
    public void ExcecaoNaoRelacionada_NaoEhMapeada()
    {
        var falha = new DbUpdateException("falha de infraestrutura", new InvalidOperationException("sem SQLite"));

        Assert.Null(ErroDePersistencia.Mapear(falha));
    }

    [Fact]
    public void ViolacaoUnicaSemEntidades_MapeiaPara409Generico()
    {
        // Entries vazio: o código é a única informação disponível, então nada de mensagem
        // específica (não há como saber qual índice falhou).
        var falha = new DbUpdateException("violação única", new SqliteException("UNIQUE constraint failed", 19, 2067));

        var resposta = ErroDePersistencia.Mapear(falha);

        Assert.NotNull(resposta);
        Assert.Equal(409, StatusDe(resposta!));
        Assert.Equal("Registro duplicado", MensagemDe(resposta!));
    }

    private static int StatusDe(IResult resultado) =>
        Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode!.Value;

    private static string? MensagemDe(IResult resultado)
    {
        var corpo = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;

        return corpo?.GetType().GetProperty("erro")?.GetValue(corpo) as string;
    }
}
