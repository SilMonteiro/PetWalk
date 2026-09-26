public class Notificacao
{
    public int Id { get; set; }
    public int DestinatarioId { get; set; }
    public TipoNotificacao Tipo { get; set; }
    public string Mensagem { get; set; } = "";
    public bool Lida { get; set; } = false;
    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
    public int PasseioId { get; set; }

     public void MarcarComoLida()
    {
        Lida = true;
    }

}
