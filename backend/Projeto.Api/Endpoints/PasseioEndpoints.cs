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
