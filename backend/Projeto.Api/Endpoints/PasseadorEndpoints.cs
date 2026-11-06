// FLX-001: listagem dos passeadores ativos para o tutor escolher. O repositório já filtra
// perfil de passeador e status ATIVO; aqui só resta reduzir ao perfil público (REG-007).
public static class PasseadorEndpoints
{
    public static void MapPasseadorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/passeadores", (IUsuarioRepositorio repositorio) =>
            Results.Ok(repositorio.ListarPasseadoresAtivos().Select(ParaResposta).ToList()));
    }

    // Exposto como internal para o teste garantir que o DTO não carrega dados sensíveis.
    internal static PasseadorPublicoResponse ParaResposta(Usuario passeador) =>
        new(passeador.Id, passeador.Nome, passeador.MediaAvaliacao, passeador.ValorHora, passeador.TipoCobranca);
}
