public interface INotificacaoRepositorio
{
    Notificacao Adicionar(Notificacao notificacao);

    Notificacao? ObterPorId(int id);

    IReadOnlyList<Notificacao> ListarPorDestinatario(int usuarioId);
}
