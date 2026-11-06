// Perfil público do passeador (FLX-001). REG-007: nada de e-mail, telefone, endereço ou senha.
public record PasseadorPublicoResponse(
    int Id,
    string Nome,
    double MediaAvaliacao,
    decimal ValorHora,
    TipoCobranca TipoCobranca);
