// FLX-005: resposta da avaliação. Sem Id de avaliação: não existe repositório de avaliações,
// então Avaliacao.DefinirId nunca é chamado e o valor seria sempre 0.
public record AvaliacaoResponse(
    int Nota,
    string Comentario,
    DateTime Data,
    ParticipantePasseioResponse Avaliado,
    double MediaAvaliacao);
