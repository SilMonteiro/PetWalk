// enviarNotificacao() e listarNaoLidas() da proposta, como serviço da Etapa 4.
public class NotificacaoServico
{
    private readonly INotificacaoRepositorio _notificacoes;

    public NotificacaoServico(INotificacaoRepositorio notificacoes)
    {
        _notificacoes = notificacoes ?? throw new ArgumentNullException(nameof(notificacoes));
    }

    public Notificacao Enviar(Usuario destinatario, Passeio passeio, TipoNotificacao tipo, DateTime agora)
    {
        var notificacao = new Notificacao(destinatario, passeio, tipo, agora);

        return _notificacoes.Adicionar(notificacao);
    }

    // Histórico do destinatário; apenasNaoLidas filtra o que ainda não foi lido.
    public IReadOnlyList<Notificacao> ListarPorUsuario(int usuarioId, bool apenasNaoLidas)
    {
        var notificacoes = _notificacoes.ListarPorDestinatario(usuarioId);

        return apenasNaoLidas ? notificacoes.Where(n => !n.Lida).ToList() : notificacoes;
    }

    public IReadOnlyList<Notificacao> ListarNaoLidas(int usuarioId) =>
        ListarPorUsuario(usuarioId, apenasNaoLidas: true);
}
