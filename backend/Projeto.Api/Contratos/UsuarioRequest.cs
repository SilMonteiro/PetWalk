public record UsuarioRequest(
    string Nome,
    string Email,
    string Telefone,
    string Endereco,
    TipoPerfil TipoPerfil,
    string Senha,
    decimal? ValorHora = null,
    TipoCobranca? TipoCobranca = null);
