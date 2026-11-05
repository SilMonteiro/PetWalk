// enviarNotificacao() e listarNaoLidas() da proposta, como serviço da Etapa 4.
public class NotificacaoServico
{
    private readonly INotificacaoRepositorio _notificacoes;

    public NotificacaoServico(INotificacaoRepositorio notificacoes)
    {
        _notificacoes = notificacoes ?? throw new ArgumentNullException(nameof(notificacoes));
    }

    public Notificacao Enviar(Usuario destinatario, Passeio passeio, TipoNotificacao tipo)
    {
        var notificacao = new Notificacao(destinatario, passeio, tipo);

        return _notificacoes.Adicionar(notificacao);
    }

    public IReadOnlyList<Notificacao> ListarNaoLidas(int usuarioId) =>
        _notificacoes.ListarPorDestinatario(usuarioId).Where(n => !n.Lida).ToList();
}
