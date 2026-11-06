// listarNaoLidas() e marcarComoLida() da proposta, expostos ao destinatário.
// A notificação nasce nos endpoints de passeio; aqui ela só é consultada e marcada como lida.
public static class NotificacaoEndpoints
{
    public static void MapNotificacaoEndpoints(this IEndpointRouteBuilder app)
    {
        // naoLidas=true filtra as pendentes; ausente devolve o histórico completo.
        app.MapGet("/usuarios/{id:int}/notificacoes", (int id, bool? naoLidas, IUsuarioRepositorio usuarios,
            NotificacaoServico notificacoes) =>
            Listar(id, naoLidas ?? false, usuarios, notificacoes));

        app.MapPost("/notificacoes/{id:int}/lida", (int id, MarcarNotificacaoLidaRequest req,
            INotificacaoRepositorio notificacoes) =>
            MarcarComoLida(id, req, notificacoes));
    }

    internal static IResult Listar(int usuarioId, bool apenasNaoLidas, IUsuarioRepositorio usuarios,
        NotificacaoServico notificacoes)
    {
        if (usuarios.ObterPorId(usuarioId) is null)
        {
            return ErroHttp.NaoEncontrado("Usuário não encontrado");
        }

        return Results.Ok(notificacoes.ListarPorUsuario(usuarioId, apenasNaoLidas).Select(ParaResposta).ToList());
    }

    // Só o destinatário pode marcar como lida. Para qualquer outro usuário o 404 é proposital:
    // responder 403 revelaria que a notificação existe.
    internal static IResult MarcarComoLida(int notificacaoId, MarcarNotificacaoLidaRequest req,
        INotificacaoRepositorio notificacoes)
    {
        var notificacao = notificacoes.ObterPorId(notificacaoId);

        if (notificacao is null || notificacao.Destinatario.Id != req.UsuarioId)
        {
            return ErroHttp.NaoEncontrado("Notificação não encontrada");
        }

        notificacao.MarcarComoLida();

        return Results.NoContent();
    }

    internal static NotificacaoResponse ParaResposta(Notificacao notificacao) =>
        new(notificacao.Id,
            notificacao.Tipo,
            notificacao.Mensagem,
            notificacao.Lida,
            notificacao.DataEnvio,
            notificacao.Passeio.Id);
}
