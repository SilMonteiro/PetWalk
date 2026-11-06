// Contrato público do pet nas respostas: expõe o TutorId, nunca a entidade Usuario (REG-007).
public record PetResponse(
    int Id,
    string Nome,
    string Especie,
    string Raca,
    Porte Porte,
    int Idade,
    string Observacoes,
    IReadOnlyList<string> Vacinas,
    int TutorId);
