// Repositório em memória do agregado Usuario (os pets vivem dentro do usuário).
// Só guarda e consulta: hash de senha e regras de login ficam no AutenticacaoServico.
public class UsuarioRepositorioEmMemoria : IUsuarioRepositorio
{
    private readonly List<Usuario> _usuarios = new();
    private int _proximoPetId = 1;

    public Usuario Adicionar(Usuario usuario)
    {
        usuario.DefinirId(_usuarios.Count + 1);
        _usuarios.Add(usuario);

        return usuario;
    }

    public Usuario? ObterPorId(int id) => _usuarios.FirstOrDefault(u => u.Id == id);

    // REG-010: o e-mail identifica o usuário, então a comparação ignora maiúsculas/minúsculas.
    public Usuario? ObterPorEmail(string email) =>
        _usuarios.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Usuario> ListarPasseadoresAtivos() =>
        _usuarios.Where(u => u.EhPasseador && u.Status == StatusUsuario.ATIVO).ToList();

    public Pet? ObterPet(int petId) => _usuarios.SelectMany(u => u.Pets).FirstOrDefault(p => p.Id == petId);

    public Pet RegistrarPet(Usuario tutor, Pet pet)
    {
        tutor.AdicionarPet(pet);

        if (pet.Id == 0)
        {
            pet.DefinirId(_proximoPetId);
            _proximoPetId++;
        }

        return pet;
    }

    // Em memória o agregado é a própria fonte: remover da coleção basta.
    public void RemoverPet(Usuario tutor, Pet pet) => tutor.RemoverPet(pet);
}
