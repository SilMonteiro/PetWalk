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

    // Veio de Passeador.AceitarPasseio (arquivo removido). Regras: REG-003 e o estado do passeio.
    public void Aceitar(Usuario quem)
    {
        if (quem is null)
        {
            throw new ArgumentNullException(nameof(quem));
        }

        if (quem != Passeador)
        {
            throw new InvalidOperationException("Passeio não pertence a este passeador");
        }

        if (!quem.EhPasseador)
        {
            throw new InvalidOperationException("Usuário não possui perfil de passeador");
        }

        if (quem.Status != StatusUsuario.ATIVO)
        {
            throw new InvalidOperationException("Usuário está bloqueado");
        }

        if (Status != StatusPasseio.SOLICITADO)
        {
            throw new InvalidOperationException("O passeio não está em estado de solicitação.");
        }

        Status = StatusPasseio.ACEITO;
    }

    // Usado pela Avaliacao para derivar o avaliado enquanto `Avaliacao.Avaliado` ainda é um Id.
    internal int ObterOutroParticipanteId(Usuario usuario) => usuario == Tutor ? Passeador.Id : Tutor.Id;

    internal void DefinirId(int id) => Id = id;
}
