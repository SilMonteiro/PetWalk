public class Avaliacao
{
    public int Id { get; private set; }
    public int Nota { get; private set; }
    public string Comentario { get; private set; } = string.Empty;
    public DateTime Data { get; private set; }
    public Usuario Avaliador { get; }
    public Passeio Passeio { get; }
    public Usuario Avaliado { get; }

    public Avaliacao(Usuario avaliador, Passeio passeio, int nota, string comentario, DateTime agora)
    {
        Avaliador = avaliador ?? throw new ArgumentNullException(nameof(avaliador));
        Passeio = passeio ?? throw new ArgumentNullException(nameof(passeio));

        if (passeio.Status != StatusPasseio.FINALIZADO)
        {
            throw new InvalidOperationException("Passeio não finalizado");
        }

        if (nota < 1 || nota > 5)
        {
            throw new ArgumentException("Nota deve estar entre 1 e 5");
        }

        if (!passeio.Participou(avaliador))
        {
            throw new InvalidOperationException("Usuário não participou deste passeio");
        }

        Nota = nota;
        Comentario = comentario ?? string.Empty;
        Data = agora;
        Avaliado = passeio.ObterOutroParticipante(avaliador);
    }

    internal void DefinirId(int id) => Id = id;
}
