// FLX-002: o tutor solicita o passeio (REG-002, REG-005, REG-008, REG-009) e o passeador
// escolhido recebe a notificação PASSEIO_SOLICITADO. O endpoint só orquestra; as regras
// continuam em Usuario.SolicitarPasseio.
public static class PasseioEndpoints
{
    public static void MapPasseioEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/passeios", (SolicitacaoPasseioRequest req, IUsuarioRepositorio usuarios,
            IPasseioRepositorio passeios, NotificacaoServico notificacoes, TimeProvider time) =>
            Solicitar(req, usuarios, passeios, notificacoes, time.GetUtcNow().UtcDateTime));

        app.MapGet("/usuarios/{id:int}/passeios", (int id, IUsuarioRepositorio usuarios, IPasseioRepositorio passeios) =>
            Listar(id, usuarios, passeios));

        // FLX-003: o passeador escolhido aceita; o tutor é notificado (REG-003).
        app.MapPost("/passeios/{id:int}/aceitar", (int id, AcaoPasseioRequest req, IPasseioRepositorio passeios,
            IUsuarioRepositorio usuarios, NotificacaoServico notificacoes, TimeProvider time) =>
            ExecutarAcao(id, req, passeios, usuarios, notificacoes, time.GetUtcNow().UtcDateTime,
                (passeio, quem) => passeio.Aceitar(quem), TipoNotificacao.PASSEIO_ACEITO));

        // FLX-009: o passeador escolhido recusa; o tutor é notificado (REG-003).
        app.MapPost("/passeios/{id:int}/recusar", (int id, AcaoPasseioRequest req, IPasseioRepositorio passeios,
            IUsuarioRepositorio usuarios, NotificacaoServico notificacoes, TimeProvider time) =>
            ExecutarAcao(id, req, passeios, usuarios, notificacoes, time.GetUtcNow().UtcDateTime,
                (passeio, quem) => passeio.Recusar(quem), TipoNotificacao.PASSEIO_RECUSADO));
    }

    // Fluxo comum às ações do passeador: localiza as partes, deixa o domínio decidir (REG-003),
    // notifica o outro participante e devolve o passeio atualizado.
    internal static IResult ExecutarAcao(
        int passeioId,
        AcaoPasseioRequest req,
        IPasseioRepositorio passeios,
        IUsuarioRepositorio usuarios,
        NotificacaoServico notificacoes,
        DateTime agora,
        Action<Passeio, Usuario> acao,
        TipoNotificacao tipoNotificacao)
    {
        var passeio = passeios.ObterPorId(passeioId);

        if (passeio is null)
        {
            return ErroHttp.NaoEncontrado("Passeio não encontrado");
        }

        var usuario = usuarios.ObterPorId(req.UsuarioId);

        if (usuario is null)
        {
            return ErroHttp.NaoEncontrado("Usuário não encontrado");
        }

        try
        {
            acao(passeio, usuario);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return ErroHttp.Erro(ex);
        }

        // Só chega aqui quem passou pela REG-003, então o outro participante é a parte a notificar.
        notificacoes.Enviar(passeio.ObterOutroParticipante(usuario), passeio, tipoNotificacao, agora);

        return Results.Ok(ParaResposta(passeio, usuario));
    }

    // Histórico do usuário: os passeios em que ele é tutor ou passeador, com o papel de cada um.
    internal static IResult Listar(int usuarioId, IUsuarioRepositorio usuarios, IPasseioRepositorio passeios)
    {
        var usuario = usuarios.ObterPorId(usuarioId);

        if (usuario is null)
        {
            return ErroHttp.NaoEncontrado("Usuário não encontrado");
        }

        return Results.Ok(passeios.ListarPorUsuario(usuarioId).Select(p => ParaResposta(p, usuario)).ToList());
    }

    // Método interno para o teste exercitar o fluxo completo sem subir a API.
    internal static IResult Solicitar(
        SolicitacaoPasseioRequest req,
        IUsuarioRepositorio usuarios,
        IPasseioRepositorio passeios,
        NotificacaoServico notificacoes,
        DateTime agora)
    {
        var tutor = usuarios.ObterPorId(req.UsuarioId);

        if (tutor is null)
        {
            return ErroHttp.NaoEncontrado("Usuário não encontrado");
        }

        var passeador = usuarios.ObterPorId(req.PasseadorId);

        if (passeador is null)
        {
            return ErroHttp.NaoEncontrado("Passeador não encontrado");
        }

        // REG-005: pet que não está no perfil do tutor chega como null e o domínio recusa com a mensagem da proposta.
        var pet = tutor.Pets.FirstOrDefault(p => p.Id == req.PetId);

        try
        {
            var passeio = tutor.SolicitarPasseio(
                pet!, passeador, req.DataHora, TimeSpan.FromMinutes(req.DuracaoMinutos), req.Local, agora);

            passeios.Adicionar(passeio);
            notificacoes.Enviar(passeador, passeio, TipoNotificacao.PASSEIO_SOLICITADO, agora);

            return Results.Created($"/passeios/{passeio.Id}", ParaResposta(passeio, tutor));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return ErroHttp.Erro(ex);
        }
    }

    // A perspectiva define o Papel; só é chamada para quem participa do passeio.
    internal static PasseioResponse ParaResposta(Passeio passeio, Usuario perspectiva) =>
        new(passeio.Id,
            passeio.Status,
            passeio.DataHora,
            (int)passeio.Duracao.TotalMinutes,
            passeio.Local,
            passeio.Valor,
            passeio.Pet.Nome,
            new ParticipantePasseioResponse(passeio.Tutor.Id, passeio.Tutor.Nome),
            new ParticipantePasseioResponse(passeio.Passeador.Id, passeio.Passeador.Nome),
            // Comparação de referência: o mesmo passeio visto pelo tutor ou pelo passeador.
            passeio.Tutor == perspectiva ? nameof(TipoPerfil.TUTOR) : nameof(TipoPerfil.PASSEADOR));
}
