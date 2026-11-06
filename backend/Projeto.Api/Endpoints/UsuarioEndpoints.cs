public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/cadastro", (UsuarioRequest req, AutenticacaoServico autenticacao) =>
        {
            try
            {
                var usuarioCriado = autenticacao.Cadastrar(
                    req.Nome, req.Email, req.Telefone, req.Endereco, req.TipoPerfil, req.Senha,
                    req.ValorHora, req.TipoCobranca);
                return Results.Ok(new { mensagem = "Usuário cadastrado com sucesso!", id = usuarioCriado.Id });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // REG-010: e-mail já cadastrado é regra de negócio, não entrada inválida.
                return Results.Conflict(new { erro = ex.Message });
            }
        });

        app.MapPost("/login", (LoginRequest req, AutenticacaoServico autenticacao) =>
        {
            try
            {
                var usuario = autenticacao.Autenticar(req.Email, req.Senha);

                if (usuario is null)
                {
                    return Results.Json(new { erro = "E-mail ou senha inválidos" }, statusCode: StatusCodes.Status401Unauthorized);
                }

                return Results.Ok(new
                {
                    mensagem = "Login realizado com sucesso!",
                    id = usuario.Id,
                    nome = usuario.Nome,
                    email = usuario.Email
                });
            }
            catch (InvalidOperationException ex)
            {
                // REG-011: só chega aqui quem acertou a senha de uma conta bloqueada.
                return Results.Json(new { erro = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        });
    }
}
