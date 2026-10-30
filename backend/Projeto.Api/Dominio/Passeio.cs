public class Passeio
{
    public int Id { get; private set; }
    public DateTime DataHora { get; private set; }
    public TimeSpan Duracao { get; private set; }
    public string Local { get; private set; } = string.Empty;

    public Usuario Tutor { get; }
    public Usuario Passeador { get; }
    public Pet Pet { get; }

    public StatusPasseio Status { get; private set; } = StatusPasseio.SOLICITADO;

    internal Passeio(Usuario tutor, Usuario passeador, Pet pet, DateTime dataHora, TimeSpan duracao, string local)
    {
        Tutor = tutor ?? throw new ArgumentNullException(nameof(tutor));
        Passeador = passeador ?? throw new ArgumentNullException(nameof(passeador));
        Pet = pet ?? throw new ArgumentNullException(nameof(pet));

        DataHora = dataHora;
        Duracao = duracao;
        Local = local ?? string.Empty;
        Status = StatusPasseio.SOLICITADO;
    }

    public bool Participou(Usuario u) => u is not null && (u == Tutor || u == Passeador);

    // Usado pela Avaliacao para derivar o avaliado enquanto `Avaliacao.Avaliado` ainda é um Id.
    internal int ObterOutroParticipanteId(Usuario usuario) => usuario == Tutor ? Passeador.Id : Tutor.Id;

    internal void DefinirId(int id) => Id = id;
}
