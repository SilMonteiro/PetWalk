using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Regressão da concorrência na média: o valor persistido é recalculado no banco, na mesma
// transação do INSERT da avaliação, e não depende do histórico carregado pela requisição.
public class MediaAvaliacaoConcorrenciaTests
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

    // Dois passeios finalizados para o mesmo passeador, cada um com um tutor próprio.
    // Os e-mails dos tutores precisam ser distintos: Usuarios.Email tem índice único.
    private static (int passeio1, int passeio2, int passeadorId) SemearPasseiosFinalizados(
        SqliteConnection conexao)
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);

        var tutor1 = Fabrica.CriarUsuario(TipoPerfil.TUTOR, "tutor1@petwalk.com");
        var pet1 = Fabrica.CriarPet(tutor1);
        tutor1.AdicionarPet(pet1);
        var passeio1 = tutor1.SolicitarPasseio(
            pet1, passeador, Fabrica.Agora.AddDays(1), TimeSpan.FromHours(1), "Rua A, 10", Fabrica.Agora);

        var tutor2 = Fabrica.CriarUsuario(TipoPerfil.TUTOR, "tutor2@petwalk.com");
        var pet2 = Fabrica.CriarPet(tutor2);
        tutor2.AdicionarPet(pet2);
        var passeio2 = tutor2.SolicitarPasseio(
            pet2, passeador, Fabrica.Agora.AddDays(2), TimeSpan.FromHours(1), "Rua B, 20", Fabrica.Agora);

        using (var escrita = CriarContextoComSchema(conexao))
        {
            escrita.AddRange(passeador, tutor1, pet1, tutor2, pet2);
            escrita.SaveChanges();

            var repositorio = new PasseioRepositorio(escrita);
            repositorio.Adicionar(passeio1);
            repositorio.Adicionar(passeio2);
            new UnidadeDeTrabalho(escrita).Salvar();
        }

        // Só passeio FINALIZADO pode ser avaliado; o estado é gravado antes das avaliações.
        using (var escrita = CriarContexto(conexao))
        {
            var repositorio = new PasseioRepositorio(escrita);

            foreach (var id in new[] { passeio1.Id, passeio2.Id })
            {
                var passeio = repositorio.ObterPorId(id)!;

                passeio.Aceitar(passeio.Passeador);
                passeio.Iniciar(passeio.Passeador);
                passeio.Finalizar(passeio.Passeador);
            }

            new UnidadeDeTrabalho(escrita).Salvar();
        }

        return (passeio1.Id, passeio2.Id, passeador.Id);
    }

    [Fact]
    public void MediaPersistida_NaoDependeDoHistoricoCarregado()
    {
        using var conexao = AbrirConexao();
        var (passeio1Id, passeio2Id, passeadorId) = SemearPasseiosFinalizados(conexao);

        // Primeira avaliação (nota 3) pelo caminho real do endpoint.
        using (var contexto = CriarContexto(conexao))
        {
            var passeio = new PasseioRepositorio(contexto).ObterParaAvaliacao(passeio1Id)!;

            passeio.Avaliar(passeio.Tutor, 3, "Ok", Fabrica.Agora);
            new UnidadeDeTrabalho(contexto).Salvar();
        }

        // Segunda avaliação (nota 5) sem o histórico do passeador carregado: a média
        // em memória enxergaria só esta avaliação, mas o valor persistido vem do banco.
        using (var contexto = CriarContexto(conexao))
        {
            var passeio = new PasseioRepositorio(contexto).ObterPorId(passeio2Id)!;

            // Premissa do teste: a leitura comum não traz avaliações recebidas.
            Assert.Empty(passeio.Passeador.AvaliacoesRecebidas);

            passeio.Avaliar(passeio.Tutor, 5, "Ótimo", Fabrica.Agora);
            new UnidadeDeTrabalho(contexto).Salvar();

            // A resposta da requisição precisa devolver a média já recalculada.
            Assert.Equal(4d, passeio.Passeador.MediaAvaliacao);
        }

        using var leitura = CriarContexto(conexao);

        Assert.Equal(4d, leitura.Usuarios.Single(u => u.Id == passeadorId).MediaAvaliacao);
        Assert.Equal(2, leitura.Avaliacoes.Count(a => a.Avaliado.Id == passeadorId));
    }

    [Fact]
    public void AvaliacoesIntercaladasEmPasseiosDiferentes_NaoSobrescrevemAMedia()
    {
        using var conexao = AbrirConexao();
        var (passeio1Id, passeio2Id, passeadorId) = SemearPasseiosFinalizados(conexao);

        // Duas requisições carregam o passeio antes de qualquer gravação: os dois snapshots
        // nascem com o mesmo histórico vazio — exatamente a corrida que causava o lost update.
        using var contextoA = CriarContexto(conexao);
        using var contextoB = CriarContexto(conexao);

        var passeioA = new PasseioRepositorio(contextoA).ObterParaAvaliacao(passeio1Id)!;
        var passeioB = new PasseioRepositorio(contextoB).ObterParaAvaliacao(passeio2Id)!;

        Assert.Empty(passeioA.Passeador.AvaliacoesRecebidas);
        Assert.Empty(passeioB.Passeador.AvaliacoesRecebidas);

        // B grava primeiro (nota 3); A grava depois (nota 5) com o snapshot antigo.
        passeioB.Avaliar(passeioB.Tutor, 3, "Ok", Fabrica.Agora);
        new UnidadeDeTrabalho(contextoB).Salvar();

        passeioA.Avaliar(passeioA.Tutor, 5, "Ótimo", Fabrica.Agora);
        new UnidadeDeTrabalho(contextoA).Salvar();

        // A última gravação recalcula sobre a tabela: 5 e 3 → média 4, sem perder a de B.
        Assert.Equal(4d, passeioA.Passeador.MediaAvaliacao);

        using var leitura = CriarContexto(conexao);

        Assert.Equal(4d, leitura.Usuarios.Single(u => u.Id == passeadorId).MediaAvaliacao);
        Assert.Equal(2, leitura.Avaliacoes.Count(a => a.Avaliado.Id == passeadorId));
    }
}
