// Quem está marcando a notificação como lida. O destinatário é validado no endpoint;
// outro usuário recebe 404 para não descobrir que a notificação existe.
public record MarcarNotificacaoLidaRequest(int UsuarioId);
