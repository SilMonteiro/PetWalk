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

            return Results.Created($"/passeios/{passeio.Id}", ParaResposta(passeio));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return ErroHttp.Erro(ex);
        }
    }

    internal static PasseioResponse ParaResposta(Passeio passeio) =>
        new(passeio.Id,
            passeio.Status,
            passeio.DataHora,
            (int)passeio.Duracao.TotalMinutes,
            passeio.Local,
            passeio.Valor,
            passeio.Pet.Nome,
            new ParticipantePasseioResponse(passeio.Tutor.Id, passeio.Tutor.Nome),
            new ParticipantePasseioResponse(passeio.Passeador.Id, passeio.Passeador.Nome));
}
