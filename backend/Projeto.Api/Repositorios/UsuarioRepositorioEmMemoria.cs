using Microsoft.AspNetCore.Identity;

// Repositório mínimo criado junto com a proteção do Usuario: a entidade não guarda mais
// a lista nem as operações de cadastro/login. A versão completa (interface, DTOs de saída
// e AutenticacaoServico) entra na Etapa 5.
public class UsuarioRepositorioEmMemoria
{
    private readonly List<Usuario> _usuarios = new();
    private readonly PasswordHasher<Usuario> _hasher = new();

    public Usuario Adicionar(string nome, string email, string telefone, string endereco, TipoPerfil tipoPerfil, string senha)
    {
        if (_usuarios.Any(u => u.Email == email))
        {
            throw new ArgumentException("E-mail já cadastrado");
        }

        // PasswordHasher ignora a instância recebida: o hash depende só da senha.
        var senhaHash = _hasher.HashPassword(null!, senha);
        var usuario = new Usuario(nome, email, telefone, endereco, tipoPerfil, senhaHash);

        usuario.DefinirId(_usuarios.Count + 1);
        _usuarios.Add(usuario);

        return usuario;
    }

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
