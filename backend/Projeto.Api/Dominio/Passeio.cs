public class Passeio
{
    private readonly List<Avaliacao> _avaliacoes = new();

    public int Id { get; private set; }
    public DateTime DataHora { get; private set; }
    public TimeSpan Duracao { get; private set; }
    public string Local { get; private set; } = string.Empty;
    public decimal Valor { get; }

    public Usuario Tutor { get; }
    public Usuario Passeador { get; }
    public Pet Pet { get; }

    public StatusPasseio Status { get; private set; } = StatusPasseio.SOLICITADO;

    public IReadOnlyList<Avaliacao> Avaliacoes => _avaliacoes;

    internal Passeio(Usuario tutor, Usuario passeador, Pet pet, DateTime dataHora, TimeSpan duracao, string local, IPoliticaDePreco politica)
    {
        Tutor = tutor ?? throw new ArgumentNullException(nameof(tutor));
        Passeador = passeador ?? throw new ArgumentNullException(nameof(passeador));
        Pet = pet ?? throw new ArgumentNullException(nameof(pet));

        if (politica is null)
        {
            throw new ArgumentNullException(nameof(politica));
        }

        DataHora = dataHora;
        Duracao = duracao;
        Local = local ?? string.Empty;
        Status = StatusPasseio.SOLICITADO;

        // Retrato do momento da solicitação: a política lê os campos já atribuídos.
        Valor = politica.Calcular(this);
    }

    public bool Participou(Usuario u) => u is not null && (u == Tutor || u == Passeador);

    // Veio de Passeador.AceitarPasseio (arquivo removido). Regras: REG-003 e o estado do passeio.
    public void Aceitar(Usuario quem)
    {
        ExigirPasseadorAtivo(quem);
        ExigirStatus(StatusPasseio.SOLICITADO);

        Status = StatusPasseio.ACEITO;
    }

    // Veio de Passeador.RecusarPasseio (arquivo removido). REG-003; RECUSADO é terminal.
    public void Recusar(Usuario quem)
    {
        ExigirPasseadorAtivo(quem);
        ExigirStatus(StatusPasseio.SOLICITADO);

        Status = StatusPasseio.RECUSADO;
    }

    // Veio de Passeador.IniciarPasseio (arquivo removido). REG-003.
    public void Iniciar(Usuario quem)
    {
        ExigirPasseadorAtivo(quem);
        ExigirStatus(StatusPasseio.ACEITO);

        Status = StatusPasseio.EM_ANDAMENTO;
    }

    // Veio de Passeador.FinalizarPasseio (arquivo removido). REG-003; FINALIZADO é terminal.
    public void Finalizar(Usuario quem)
    {
        ExigirPasseadorAtivo(quem);
        ExigirStatus(StatusPasseio.EM_ANDAMENTO);

        Status = StatusPasseio.FINALIZADO;
    }

    // REG-004: tutor ou passeador podem cancelar, mas só enquanto SOLICITADO ou ACEITO.
    public void Cancelar(Usuario quem)
    {
        if (!Participou(quem))
        {
            throw new InvalidOperationException("Usuário não participou deste passeio.");
        }

        if (Status is not (StatusPasseio.SOLICITADO or StatusPasseio.ACEITO))
        {
            throw new InvalidOperationException("Passeio não pode ser cancelado no estado atual.");
        }

        Status = StatusPasseio.CANCELADO;
    }

    // REG-001: cada participante pode avaliar o outro uma única vez, após FINALIZADO.
    // Devolve a avaliação criada para o chamador não depender da ordem da lista.
    public Avaliacao Avaliar(Usuario avaliador, int nota, string comentario, DateTime agora)
    {
        foreach (var existente in _avaliacoes)
        {
            if (existente.Avaliador == avaliador)
            {
                throw new InvalidOperationException("Passeio já avaliado por este usuário");
            }
        }

        var avaliacao = new Avaliacao(avaliador, this, nota, comentario, agora);
        _avaliacoes.Add(avaliacao);
        avaliacao.Avaliado.RegistrarAvaliacaoRecebida(avaliacao);

        return avaliacao;
    }

    // REG-003: somente o passeador escolhido, com perfil de passeador e ativo, pode agir no passeio.
    private void ExigirPasseadorAtivo(Usuario quem)
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

        if (quem.Status == StatusUsuario.BLOQUEADO)
        {
            throw new InvalidOperationException("Usuário está bloqueado");
        }
    }

    private void ExigirStatus(StatusPasseio esperado)
    {
        if (Status != esperado)
        {
            throw new InvalidOperationException("Transição não permitida a partir do estado atual");
        }
    }

    internal Usuario ObterOutroParticipante(Usuario usuario) => usuario == Tutor ? Passeador : Tutor;

    internal void DefinirId(int id) => Id = id;
}
