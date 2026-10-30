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

    internal void DefinirId(int id) => Id = id;
}
