public class Avaliacao
{
    public int Id { get; private set; }
    public int Nota { get; private set; }
    public string Comentario { get; private set; } = string.Empty;
    public DateTime Data { get; private set; }
    public Usuario Avaliador { get; }

    // Temporário: vira `Passeio Passeio { get; }` quando a classe Passeio for protegida.
    public int PasseioId { get; private set; }

    // Temporário: vira `Usuario Avaliado { get; }` quando o Passeio referenciar Usuario.
    public int AvaliadoId { get; private set; }

    public Avaliacao(Usuario avaliador, Passeio passeio, int nota, string comentario)
    {
        Avaliador = avaliador ?? throw new ArgumentNullException(nameof(avaliador));
        PasseioId = (passeio ?? throw new ArgumentNullException(nameof(passeio))).Id;

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
        Data = DateTime.UtcNow;
        AvaliadoId = passeio.ObterOutroParticipanteId(avaliador);
    }

    internal void DefinirId(int id) => Id = id;
}
