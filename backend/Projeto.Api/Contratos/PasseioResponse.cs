// Participante do passeio exposto na resposta: só Id e Nome (REG-007).
public record ParticipantePasseioResponse(int Id, string Nome);

// FLX-002: Local é o informado na solicitação, nunca o endereço do perfil (REG-007).
public record PasseioResponse(
    int Id,
    StatusPasseio Status,
    DateTime DataHora,
    int DuracaoMinutos,
    string Local,
    decimal Valor,
    string NomePet,
    ParticipantePasseioResponse Tutor,
    ParticipantePasseioResponse Passeador);
