using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Concorrência nas transições: Status é token de concorrência, então duas requisições que
// leram o mesmo estado não conseguem gravar transições incompatíveis. O perdedor recebe
// DbUpdateConcurrencyException e a transação leva junto a notificação correspondente.
public class PasseioConcorrenciaTests
{
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

    private static NotificacaoServico ServicoDeNotificacao(PetWalkDbContext contexto) =>
        new(new NotificacaoRepositorio(contexto));

    private static int SemearPasseioSolicitado(SqliteConnection conexao)
    {
        var (tutor, passeador, pet, passeio) = Fabrica.CriarPasseioSolicitado();

        using var escrita = CriarContextoComSchema(conexao);
        escrita.AddRange(tutor, pet, passeador);
        escrita.SaveChanges();

        new PasseioRepositorio(escrita).Adicionar(passeio);
        new UnidadeDeTrabalho(escrita).Salvar();

        return passeio.Id;
    }

    private static int SemearPasseioAceito(SqliteConnection conexao)
    {
        var passeioId = SemearPasseioSolicitado(conexao);

        using var contexto = CriarContexto(conexao);
        var passeio = new PasseioRepositorio(contexto).ObterPorId(passeioId)!;

        passeio.Aceitar(passeio.Passeador);
        new UnidadeDeTrabalho(contexto).Salvar();

        return passeioId;
    }

    private static int SemearPasseioEmAndamento(SqliteConnection conexao)
    {
        var passeioId = SemearPasseioAceito(conexao);

        using var contexto = CriarContexto(conexao);
        var passeio = new PasseioRepositorio(contexto).ObterPorId(passeioId)!;

        passeio.Iniciar(passeio.Passeador);
        new UnidadeDeTrabalho(contexto).Salvar();

        return passeioId;
    }

    [Fact]
    public void CancelarEAceitarSimultaneos_PersistemUmaTransicaoENotificacao()
    {
        using var conexao = AbrirConexao();
        var passeioId = SemearPasseioSolicitado(conexao);

        using var contextoCancelar = CriarContexto(conexao);
        using var contextoAceitar = CriarContexto(conexao);

        var paraCancelar = new PasseioRepositorio(contextoCancelar).ObterPorId(passeioId)!;
        var paraAceitar = new PasseioRepositorio(contextoAceitar).ObterPorId(passeioId)!;

        // As duas requisições leram o mesmo estado antes de qualquer gravação.
        Assert.Equal(StatusPasseio.SOLICITADO, paraCancelar.Status);
        Assert.Equal(StatusPasseio.SOLICITADO, paraAceitar.Status);

        // Cancelamento grava primeiro: status e notificação na mesma unidade de trabalho.
        paraCancelar.Cancelar(paraCancelar.Tutor);
        ServicoDeNotificacao(contextoCancelar).Enviar(
            paraCancelar.Passeador, paraCancelar, TipoNotificacao.PASSEIO_CANCELADO, Fabrica.Agora);
        new UnidadeDeTrabalho(contextoCancelar).Salvar();

        // O aceite nasceu do snapshot antigo: o token barra a sobrescrita.
        paraAceitar.Aceitar(paraAceitar.Passeador);
        ServicoDeNotificacao(contextoAceitar).Enviar(
            paraAceitar.Tutor, paraAceitar, TipoNotificacao.PASSEIO_ACEITO, Fabrica.Agora);

        Assert.Throws<DbUpdateConcurrencyException>(
            () => new UnidadeDeTrabalho(contextoAceitar).Salvar());

        using var leitura = CriarContexto(conexao);

        Assert.Equal(
            StatusPasseio.CANCELADO,
            leitura.Passeios.Single(p => p.Id == passeioId).Status);

        // A notificação do aceite foi revertida junto com o status: só a do cancelamento ficou.
        var notificacoes = new NotificacaoRepositorio(leitura)
            .ListarPorDestinatario(paraCancelar.Passeador.Id);

        var unica = Assert.Single(notificacoes);
        Assert.Equal(TipoNotificacao.PASSEIO_CANCELADO, unica.Tipo);

        // O aceite perdedor (notificação para o tutor) não foi persistido.
        Assert.Empty(new NotificacaoRepositorio(leitura)
            .ListarPorDestinatario(paraCancelar.Tutor.Id));
    }

    [Fact]
    public void CancelarEIniciarSimultaneos_ApenasUmaTransicaoPersiste()
    {
        using var conexao = AbrirConexao();
        var passeioId = SemearPasseioAceito(conexao);

        using var contextoCancelar = CriarContexto(conexao);
        using var contextoIniciar = CriarContexto(conexao);

        var paraCancelar = new PasseioRepositorio(contextoCancelar).ObterPorId(passeioId)!;
        var paraIniciar = new PasseioRepositorio(contextoIniciar).ObterPorId(passeioId)!;

        Assert.Equal(StatusPasseio.ACEITO, paraCancelar.Status);
        Assert.Equal(StatusPasseio.ACEITO, paraIniciar.Status);

        paraCancelar.Cancelar(paraCancelar.Tutor);
        ServicoDeNotificacao(contextoCancelar).Enviar(
            paraCancelar.Passeador, paraCancelar, TipoNotificacao.PASSEIO_CANCELADO, Fabrica.Agora);
        new UnidadeDeTrabalho(contextoCancelar).Salvar();

        paraIniciar.Iniciar(paraIniciar.Passeador);
        ServicoDeNotificacao(contextoIniciar).Enviar(
            paraIniciar.Tutor, paraIniciar, TipoNotificacao.PASSEIO_INICIADO, Fabrica.Agora);

        Assert.Throws<DbUpdateConcurrencyException>(
            () => new UnidadeDeTrabalho(contextoIniciar).Salvar());

        using var leitura = CriarContexto(conexao);

        Assert.Equal(
            StatusPasseio.CANCELADO,
            leitura.Passeios.Single(p => p.Id == passeioId).Status);

        // O tutor não recebeu PASSEIO_INICIADO: a notificação do perdedor foi revertida.
        Assert.Empty(new NotificacaoRepositorio(leitura)
            .ListarPorDestinatario(paraCancelar.Tutor.Id));

        // O cancelamento vencedor notificou o passeador.
        var cancelamento = Assert.Single(new NotificacaoRepositorio(leitura)
            .ListarPorDestinatario(paraCancelar.Passeador.Id));
        Assert.Equal(TipoNotificacao.PASSEIO_CANCELADO, cancelamento.Tipo);
    }

    [Fact]
    public void FinalizarSimultaneamente_PersisteUmaUnicaNotificacao()
    {
        using var conexao = AbrirConexao();
        var passeioId = SemearPasseioEmAndamento(conexao);

        using var contextoA = CriarContexto(conexao);
        using var contextoB = CriarContexto(conexao);

        var passeioA = new PasseioRepositorio(contextoA).ObterPorId(passeioId)!;
        var passeioB = new PasseioRepositorio(contextoB).ObterPorId(passeioId)!;

        Assert.Equal(StatusPasseio.EM_ANDAMENTO, passeioA.Status);
        Assert.Equal(StatusPasseio.EM_ANDAMENTO, passeioB.Status);

        passeioA.Finalizar(passeioA.Passeador);
        ServicoDeNotificacao(contextoA).Enviar(
            passeioA.Tutor, passeioA, TipoNotificacao.PASSEIO_FINALIZADO, Fabrica.Agora);
        new UnidadeDeTrabalho(contextoA).Salvar();

        passeioB.Finalizar(passeioB.Passeador);
        ServicoDeNotificacao(contextoB).Enviar(
            passeioB.Tutor, passeioB, TipoNotificacao.PASSEIO_FINALIZADO, Fabrica.Agora);

        Assert.Throws<DbUpdateConcurrencyException>(
            () => new UnidadeDeTrabalho(contextoB).Salvar());

        using var leitura = CriarContexto(conexao);

        Assert.Equal(
            StatusPasseio.FINALIZADO,
            leitura.Passeios.Single(p => p.Id == passeioId).Status);

        // Estado compatível, mas a notificação não pode duplicar.
        Assert.Single(new NotificacaoRepositorio(leitura)
            .ListarPorDestinatario(passeioA.Tutor.Id));
    }

    [Fact]
    public void TransicaoSequencialInvalida_Continua422()
    {
        using var conexao = AbrirConexao();
        var passeioId = SemearPasseioAceito(conexao);

        using var contexto = CriarContexto(conexao);
        var passeio = new PasseioRepositorio(contexto).ObterPorId(passeioId)!;

        // Aceitar de novo, agora em um contexto novo que já enxerga ACEITO: a regra de
        // domínio continua barrando antes do SaveChanges, sem envolver o token.
        var resultado = PasseioEndpoints.ExecutarAcao(
            passeioId,
            new AcaoPasseioRequest(passeio.Passeador.Id),
            new PasseioRepositorio(contexto),
            new UsuarioRepositorio(contexto),
            ServicoDeNotificacao(contexto),
            new UnidadeDeTrabalho(contexto),
            Fabrica.Agora,
            (p, quem) => p.Aceitar(quem),
            TipoNotificacao.PASSEIO_ACEITO);

        Assert.Equal(422, Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode);

        var corpo = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;
        Assert.Equal(
            "Transição não permitida a partir do estado atual",
            corpo?.GetType().GetProperty("erro")?.GetValue(corpo));

        Assert.Empty(new NotificacaoRepositorio(contexto)
            .ListarPorDestinatario(passeio.Tutor.Id));
    }
}
