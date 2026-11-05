using Microsoft.AspNetCore.Identity;

// Repositório em memória do agregado Usuario (os pets vivem dentro do usuário).
// A versão completa (DTOs de saída e AutenticacaoServico) entra na Etapa 5.
public class UsuarioRepositorioEmMemoria : IUsuarioRepositorio
{
    private readonly List<Usuario> _usuarios = new();
    private readonly PasswordHasher<Usuario> _hasher = new();
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

    // TEMPORÁRIO: removido na fatia 5. Mantém /cadastro funcionando até o cadastro sair do repositório.
    public Usuario Adicionar(string nome, string email, string telefone, string endereco, TipoPerfil tipoPerfil, string senha)
    {
        if (_usuarios.Any(u => u.Email == email))
        {
            throw new ArgumentException("E-mail já cadastrado");
        }

        // PasswordHasher ignora a instância recebida: o hash depende só da senha.
        var senhaHash = _hasher.HashPassword(null!, senha);
        var usuario = new Usuario(nome, email, telefone, endereco, tipoPerfil, senhaHash);

        return Adicionar(usuario);
    }

    // TEMPORÁRIO: removido na fatia 5. Mantém /login funcionando até o AutenticacaoServico existir.
    public Usuario? Autenticar(string email, string senha)
    {
        var usuario = _usuarios.FirstOrDefault(u => u.Email == email);

        if (usuario is null)
        {
            return null;
        }

        var resultado = _hasher.VerifyHashedPassword(null!, usuario.SenhaHash, senha);

        return resultado == PasswordVerificationResult.Failed ? null : usuario;
    }
}
