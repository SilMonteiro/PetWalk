// FLX-002: o tutor solicita o passeio (REG-002, REG-005, REG-008, REG-009) e o passeador
// escolhido recebe a notificação PASSEIO_SOLICITADO. O endpoint só orquestra; as regras
// continuam em Usuario.SolicitarPasseio.
public static class PasseioEndpoints
{
    public static void MapPasseioEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/passeios", (SolicitacaoPasseioRequest req, IUsuarioRepositorio usuarios,
            IPasseioRepositorio passeios, NotificacaoServico notificacoes, IUnidadeDeTrabalho unidade,
            TimeProvider time) =>
            Solicitar(req, usuarios, passeios, notificacoes, unidade, time.GetUtcNow().UtcDateTime));

        app.MapGet("/usuarios/{id:int}/passeios", (int id, IUsuarioRepositorio usuarios, IPasseioRepositorio passeios) =>
            Listar(id, usuarios, passeios));

        // FLX-003: o passeador escolhido aceita; o tutor é notificado (REG-003).
        app.MapPost("/passeios/{id:int}/aceitar", (int id, AcaoPasseioRequest req, IPasseioRepositorio passeios,
            IUsuarioRepositorio usuarios, NotificacaoServico notificacoes, IUnidadeDeTrabalho unidade,
            TimeProvider time) =>
            ExecutarAcao(id, req, passeios, usuarios, notificacoes, unidade, time.GetUtcNow().UtcDateTime,
                (passeio, quem) => passeio.Aceitar(quem), TipoNotificacao.PASSEIO_ACEITO));

        // FLX-009: o passeador escolhido recusa; o tutor é notificado (REG-003).
        app.MapPost("/passeios/{id:int}/recusar", (int id, AcaoPasseioRequest req, IPasseioRepositorio passeios,
            IUsuarioRepositorio usuarios, NotificacaoServico notificacoes, IUnidadeDeTrabalho unidade,
            TimeProvider time) =>
            ExecutarAcao(id, req, passeios, usuarios, notificacoes, unidade, time.GetUtcNow().UtcDateTime,
                (passeio, quem) => passeio.Recusar(quem), TipoNotificacao.PASSEIO_RECUSADO));

        // FLX-008: o passeador escolhido inicia o passeio aceito; o tutor é notificado (REG-003).
        app.MapPost("/passeios/{id:int}/iniciar", (int id, AcaoPasseioRequest req, IPasseioRepositorio passeios,
            IUsuarioRepositorio usuarios, NotificacaoServico notificacoes, IUnidadeDeTrabalho unidade,
            TimeProvider time) =>
            ExecutarAcao(id, req, passeios, usuarios, notificacoes, unidade, time.GetUtcNow().UtcDateTime,
                (passeio, quem) => passeio.Iniciar(quem), TipoNotificacao.PASSEIO_INICIADO));

        // FLX-004: o passeador escolhido finaliza o passeio em andamento; o tutor é notificado (REG-003).
        app.MapPost("/passeios/{id:int}/finalizar", (int id, AcaoPasseioRequest req, IPasseioRepositorio passeios,
            IUsuarioRepositorio usuarios, NotificacaoServico notificacoes, IUnidadeDeTrabalho unidade,
            TimeProvider time) =>
            ExecutarAcao(id, req, passeios, usuarios, notificacoes, unidade, time.GetUtcNow().UtcDateTime,
                (passeio, quem) => passeio.Finalizar(quem), TipoNotificacao.PASSEIO_FINALIZADO));

        // FLX-009: tutor ou passeador cancela antes de começar; o outro participante é notificado (REG-004).
        app.MapPost("/passeios/{id:int}/cancelar", (int id, AcaoPasseioRequest req, IPasseioRepositorio passeios,
            IUsuarioRepositorio usuarios, NotificacaoServico notificacoes, IUnidadeDeTrabalho unidade,
            TimeProvider time) =>
            ExecutarAcao(id, req, passeios, usuarios, notificacoes, unidade, time.GetUtcNow().UtcDateTime,
                (passeio, quem) => passeio.Cancelar(quem), TipoNotificacao.PASSEIO_CANCELADO));

        // FLX-005: cada participante avalia a outra parte depois do passeio finalizado (REG-001).
        app.MapPost("/passeios/{id:int}/avaliacoes", (int id, AvaliacaoRequest req, IPasseioRepositorio passeios,
            IUsuarioRepositorio usuarios, IUnidadeDeTrabalho unidade, TimeProvider time) =>
            Avaliar(id, req, passeios, usuarios, unidade, time.GetUtcNow().UtcDateTime));
    }

    // REG-001: passeio FINALIZADO, uma avaliação por usuário, nota 1..5 e participação continuam
    // sendo decididos pelo domínio; aqui só se resolvem as partes e se monta a resposta.
    internal static IResult Avaliar(int passeioId, AvaliacaoRequest req, IPasseioRepositorio passeios,
        IUsuarioRepositorio usuarios, IUnidadeDeTrabalho unidade, DateTime agora)
    {
        var passeio = passeios.ObterPorId(passeioId);

        if (passeio is null)
        {
            return ErroHttp.NaoEncontrado("Passeio não encontrado");
        }

        var avaliador = usuarios.ObterPorId(req.UsuarioId);

        if (avaliador is null)
        {
            return ErroHttp.NaoEncontrado("Usuário não encontrado");
        }

        try
        {
            var avaliacao = passeio.Avaliar(avaliador, req.Nota, req.Comentario, agora);

            unidade.Salvar();

            return Results.Json(ParaResposta(avaliacao), statusCode: StatusCodes.Status201Created);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return ErroHttp.Erro(ex);
        }
    }

    // Fluxo comum às ações do passeador: localiza as partes, deixa o domínio decidir (REG-003),
    // notifica o outro participante e devolve o passeio atualizado.
    internal static IResult ExecutarAcao(
        int passeioId,
        AcaoPasseioRequest req,
        IPasseioRepositorio passeios,
        IUsuarioRepositorio usuarios,
        NotificacaoServico notificacoes,
        IUnidadeDeTrabalho unidade,
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

        // Estado do passeio e notificação mudaram juntos: uma única gravação no fim do fluxo.
        unidade.Salvar();

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
        IUnidadeDeTrabalho unidade,
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

            // Passeio e notificação nascem juntos: uma única gravação no fim do fluxo.
            unidade.Salvar();

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

    // Avaliacao.Id fica fora do contrato: não existe repositório de avaliações, então DefinirId
    // nunca é chamado e o Id seria sempre 0. Quando houver repositório, o Id entra aqui.
    internal static AvaliacaoResponse ParaResposta(Avaliacao avaliacao) =>
        new(avaliacao.Nota,
            avaliacao.Comentario,
            avaliacao.Data,
            new ParticipantePasseioResponse(avaliacao.Avaliado.Id, avaliacao.Avaliado.Nome),
            avaliacao.Avaliado.MediaAvaliacao);
}
