// Espelho público da Notificacao: só os campos que o destinatário precisa ver.
public record NotificacaoResponse(
    int Id,
    TipoNotificacao Tipo,
    string Mensagem,
    bool Lida,
    DateTime DataEnvio,
    int PasseioId);
