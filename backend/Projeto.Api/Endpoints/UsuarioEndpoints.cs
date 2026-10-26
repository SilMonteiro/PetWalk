public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/cadastro", (UsuarioRequest req) =>
        {
            try
            {
                var usuarioCriado = Usuario.Cadastrar(req.Nome, req.Email, req.Telefone, req.Endereco, req.TipoPerfil, req.Senha);
                return Results.Ok(new { mensagem = "Usuário cadastrado com sucesso!", id = usuarioCriado.Id });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        app.MapPost("/login", (LoginRequest req) =>
        {
            var usuario = Usuario.Login(req.Email, req.Senha);

            if (usuario == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                mensagem = "Login realizado com sucesso!",
                id = usuario.Id,
                nome = usuario.Nome,
                email = usuario.Email
            });
        });
    }
}
