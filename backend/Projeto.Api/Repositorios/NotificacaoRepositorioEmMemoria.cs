// Repositório mínimo da Etapa 4: a interface (INotificacaoRepositorio) entra na fatia 6.
public class NotificacaoRepositorioEmMemoria
{
    private readonly List<Notificacao> _notificacoes = new();

    public Notificacao Adicionar(Notificacao notificacao)
    {
        notificacao.DefinirId(_notificacoes.Count + 1);
        _notificacoes.Add(notificacao);

        return notificacao;
    }

    public IReadOnlyList<Notificacao> ListarPorDestinatario(int usuarioId) =>
        _notificacoes.Where(n => n.Destinatario.Id == usuarioId).ToList();
}
