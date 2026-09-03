
public class AvaliacaoPasseador
{
    public int Id { get; set; }
    public int Nota { get; set; } = 0;
    public string Comentario { get; set; } = "";
    public DateTime Data { get; set; } = DateTime.UtcNow;

    public int AvaliadorId { get; set; }
    public int AvaliadoId { get; set; }
    public int PasseioId { get; set; }

}