public class Usuario
{
    private readonly List<Pet> _pets = new();
    private string _endereco = string.Empty;

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public TipoPerfil TipoPerfil { get; private set; }
    public StatusUsuario Status { get; private set; } = StatusUsuario.ATIVO;
    public string SenhaHash { get; private set; } = string.Empty;
    public decimal ValorHora { get; private set; }
    public TipoCobranca TipoCobranca { get; private set; } = TipoCobranca.POR_HORA;
    public double MediaAvaliacao { get; private set; }

    public IReadOnlyList<Pet> Pets => _pets;

    public bool EhTutor => TipoPerfil is TipoPerfil.TUTOR or TipoPerfil.AMBOS;
    public bool EhPasseador => TipoPerfil is TipoPerfil.PASSEADOR or TipoPerfil.AMBOS;

    public Usuario(string nome, string email, string telefone, string endereco, TipoPerfil tipoPerfil, string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senhaHash))
        {
            throw new ArgumentException("Nome, e-mail e senha são obrigatórios.");
        }

        Nome = nome;
        Email = email;
        Telefone = telefone ?? string.Empty;
        _endereco = endereco ?? string.Empty;
        TipoPerfil = tipoPerfil;
        SenhaHash = senhaHash;
        Status = StatusUsuario.ATIVO;
    }

    // REG-007: o endereço é privado e só o próprio usuário pode consultá-lo.
    public string ObterEnderecoPara(Usuario solicitante)
    {
        if (solicitante != this)
        {
            throw new InvalidOperationException("Acesso não autorizado ao endereço do perfil");
        }

        return _endereco;
    }

    public void AdicionarPet(Pet pet)
    {
        if (pet is null)
        {
            throw new ArgumentNullException(nameof(pet));
        }

        // REG-005: só o tutor dono do pet pode vinculá-lo ao próprio perfil.
        if (pet.Tutor != this)
        {
            throw new InvalidOperationException("O pet informado não pertence ao tutor solicitante");
        }

        if (!_pets.Contains(pet))
        {
            _pets.Add(pet);
        }
    }

    public void RemoverPet(Pet pet)
    {
        if (pet is null)
        {
            throw new ArgumentNullException(nameof(pet));
        }

        if (!_pets.Remove(pet))
        {
            throw new InvalidOperationException("O pet informado não pertence ao tutor solicitante");
        }
    }

    // Veio de Tutor.SolicitarPasseio (arquivo removido). Regras: REG-002, REG-005, REG-008 e REG-009.
    public Passeio SolicitarPasseio(Pet pet, Usuario passeador, DateTime dataHora, TimeSpan duracao, string local, DateTime agora)
    {
        if (!EhTutor)
        {
            throw new InvalidOperationException("Usuário não possui perfil de tutor");
        }

        if (pet is null || !_pets.Contains(pet))
        {
            throw new InvalidOperationException("O pet informado não pertence ao tutor solicitante");
        }

        if (passeador is null)
        {
            throw new ArgumentNullException(nameof(passeador));
        }

        if (!passeador.EhPasseador)
        {
            throw new InvalidOperationException("Usuário não possui perfil de passeador");
        }

        if (passeador.Status != StatusUsuario.ATIVO)
        {
            throw new InvalidOperationException("Usuário está bloqueado");
        }

        if (dataHora.ToUniversalTime() <= agora.ToUniversalTime())
        {
            throw new InvalidOperationException("A data e hora do passeio devem ser futuras");
        }

        // Única escolha da política a partir do TipoCobranca do passeador.
        var politica = PoliticaDePrecoFabrica.Para(passeador.TipoCobranca);

        return new Passeio(this, passeador, pet, dataHora, duracao, local, politica);
    }

    // Etapa 6: única forma de configurar a cobrança usada pela política de preço.
    public void DefinirCobranca(decimal valorHora, TipoCobranca tipoCobranca)
    {
        if (!EhPasseador)
        {
            throw new InvalidOperationException("Usuário não possui perfil de passeador");
        }

        if (valorHora <= 0)
        {
            throw new ArgumentException("Valor por hora deve ser maior que zero.");
        }

        if (!Enum.IsDefined(tipoCobranca))
        {
            throw new ArgumentException("Tipo de cobrança inválido.");
        }

        ValorHora = valorHora;
        TipoCobranca = tipoCobranca;
    }

    public void Bloquear() => Status = StatusUsuario.BLOQUEADO;

    public void Desbloquear() => Status = StatusUsuario.ATIVO;

    // REG-001: média das avaliações em que este usuário é o avaliado.
    internal void RecalcularMedia(IEnumerable<Avaliacao> avaliacoes)
    {
        var notas = avaliacoes.Where(a => a.Avaliado == this).Select(a => a.Nota).ToList();
        MediaAvaliacao = notas.Count == 0 ? 0d : notas.Average();
    }

    internal void DefinirId(int id) => Id = id;
}
