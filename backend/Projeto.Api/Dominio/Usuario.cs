
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = ""; 
    public TipoPerfil TipoPerfil { get; set; } = TipoPerfil.TUTOR;
    public StatusUsuario Status { get; set; } = StatusUsuario.ATIVO;
    public string Senha { get; set; } = "";

    private static List<Usuario> listaUsuarios = new List<Usuario>();

    public static Usuario Cadastrar(string nome, string email, string telefone, string endereco, TipoPerfil tipoPerfil, string senha)
    {
        if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
        {
            throw new ArgumentException("Nome, e-mail e senha são obrigatórios.");
        }

        Usuario novoUsuario = new Usuario
        {
            Id = listaUsuarios.Count + 1,
            Nome = nome,
            Email = email,
            Telefone = telefone,
            Endereco = endereco,
            TipoPerfil = tipoPerfil,
            Senha = senha,
            Status = StatusUsuario.ATIVO
        };

        listaUsuarios.Add(novoUsuario);

        return novoUsuario;
    }

    public static List<Usuario> ObterTodos()
    {
        return listaUsuarios;
    }

}
