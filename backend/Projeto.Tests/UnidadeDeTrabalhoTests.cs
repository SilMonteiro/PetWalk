using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// IUnidadeDeTrabalho: confirma que Salvar() grava as alterações no mesmo PetWalkDbContext
// recebido no construtor, sem abrir conexão nem criar schema por conta própria.
public class UnidadeDeTrabalhoTests
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

    [Fact]
    public void Salvar_PersisteEntidadeInserida()
    {
        using var conexao = AbrirConexao();
        int usuarioId;

        using (var escrita = CriarContexto(conexao))
        {
            escrita.Database.EnsureCreated();

            var usuario = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
            escrita.Add(usuario);

            new UnidadeDeTrabalho(escrita).Salvar();

            // O Id nasce no banco: só existe se o SaveChanges realmente aconteceu.
            Assert.NotEqual(0, usuario.Id);
            usuarioId = usuario.Id;
        }

        using var leitura = CriarContexto(conexao);
        var recarregado = Assert.Single(leitura.Usuarios, u => u.Id == usuarioId);

        Assert.Equal("tutor@petwalk.com", recarregado.Email);
        Assert.Equal(StatusUsuario.ATIVO, recarregado.Status);
    }

    [Fact]
    public void Salvar_PersisteAlteracaoDeEntidadeRastreada()
    {
        using var conexao = AbrirConexao();
        int usuarioId;

        using (var escrita = CriarContexto(conexao))
        {
            escrita.Database.EnsureCreated();

            var unidade = new UnidadeDeTrabalho(escrita);
            var usuario = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

            escrita.Add(usuario);
            unidade.Salvar();

            usuarioId = usuario.Id;

            // Mudança feita pelo domínio, sem passar por repositório: só o SaveChanges grava.
            usuario.Bloquear();
            unidade.Salvar();
        }

        using var leitura = CriarContexto(conexao);

        Assert.Equal(StatusUsuario.BLOQUEADO, leitura.Usuarios.Single(u => u.Id == usuarioId).Status);
    }

    [Fact]
    public void Salvar_UsaOMesmoContextoRecebidoNoConstrutor()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        contexto.Database.EnsureCreated();

        var usuario = Fabrica.CriarUsuario(TipoPerfil.AMBOS);
        contexto.Add(usuario);

        Assert.Equal(EntityState.Added, contexto.Entry(usuario).State);

        new UnidadeDeTrabalho(contexto).Salvar();

        // Se Salvar() usasse outro contexto, esta entrada continuaria Added aqui.
        Assert.Equal(EntityState.Unchanged, contexto.Entry(usuario).State);
        Assert.NotEqual(0, usuario.Id);

        using var leitura = CriarContexto(conexao);
        Assert.NotNull(leitura.Usuarios.SingleOrDefault(u => u.Id == usuario.Id));
    }

    [Fact]
    public void Salvar_SemAlteracoes_NaoLancaErro()
    {
        using var conexao = AbrirConexao();
        using var contexto = CriarContexto(conexao);

        contexto.Database.EnsureCreated();

        var unidade = new UnidadeDeTrabalho(contexto);

        Assert.Null(Record.Exception(unidade.Salvar));
    }
}
