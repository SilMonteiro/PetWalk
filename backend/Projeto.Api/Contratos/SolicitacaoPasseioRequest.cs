public record SolicitacaoPasseioRequest(
    int UsuarioId,
    int PetId,
    int PasseadorId,
    DateTime DataHora,
    int DuracaoMinutos,
    string Local);
