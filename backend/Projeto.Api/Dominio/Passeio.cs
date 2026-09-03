public class Passeio
{
    public int Id { get; set; }
    public DateTime DataHora { get; set; } = ;
    public TimeSpan Duracao { get; set; } = ;
    public string Local { get; set; } = "";

    public int TutorId { get; set; }
    public int PasseadorId { get; set; }
    public int PetId { get; set; }

    public StatusPasseio Status { get; set; } = StatusPasseio.SOLICITADO;
}