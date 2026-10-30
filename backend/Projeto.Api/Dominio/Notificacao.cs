public class Notificacao
{
    public int Id { get; private set; }
    public Usuario Destinatario { get; }
    public TipoNotificacao Tipo { get; }
    public string Mensagem { get; private set; } = string.Empty;
    public bool Lida { get; private set; }
    public DateTime DataEnvio { get; private set; }

    // Temporário: vira `Passeio Passeio { get; }` quando a classe Passeio for protegida.
    public int PasseioId { get; private set; }

    public Notificacao(Usuario destinatario, Passeio passeio, TipoNotificacao tipo)
    {
        Destinatario = destinatario ?? throw new ArgumentNullException(nameof(destinatario));
        PasseioId = (passeio ?? throw new ArgumentNullException(nameof(passeio))).Id;

        Tipo = tipo;
        Mensagem = GerarMensagem(tipo);
        Lida = false;
        DataEnvio = DateTime.UtcNow;
    }

    public void MarcarComoLida() => Lida = true;

    internal void DefinirId(int id) => Id = id;

    private static string GerarMensagem(TipoNotificacao tipo) => tipo switch
    {
        TipoNotificacao.PASSEIO_SOLICITADO => "Novo passeio solicitado.",
        TipoNotificacao.PASSEIO_ACEITO => "Seu passeio foi aceito.",
        TipoNotificacao.PASSEIO_RECUSADO => "Seu passeio foi recusado.",
        TipoNotificacao.PASSEIO_INICIADO => "Seu passeio foi iniciado.",
        TipoNotificacao.PASSEIO_FINALIZADO => "Seu passeio foi finalizado.",
        TipoNotificacao.PASSEIO_CANCELADO => "O passeio foi cancelado.",
        _ => throw new ArgumentException("Tipo de notificação inválido.")
    };
}
