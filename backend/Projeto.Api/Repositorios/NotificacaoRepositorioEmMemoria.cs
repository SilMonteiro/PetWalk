// Repositório em memória das notificações; o Id é atribuído aqui, nunca pela entidade.
public class NotificacaoRepositorioEmMemoria : INotificacaoRepositorio
{
    private readonly List<Notificacao> _notificacoes = new();

    public Notificacao Adicionar(Notificacao notificacao)
    {
        notificacao.DefinirId(_notificacoes.Count + 1);
        _notificacoes.Add(notificacao);

        return notificacao;
    }

    public Notificacao? ObterPorId(int id) => _notificacoes.FirstOrDefault(n => n.Id == id);

    public IReadOnlyList<Notificacao> ListarPorDestinatario(int usuarioId) =>
        _notificacoes.Where(n => n.Destinatario.Id == usuarioId).ToList();
}
