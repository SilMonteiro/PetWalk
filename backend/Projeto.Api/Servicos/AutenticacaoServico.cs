using Microsoft.AspNetCore.Identity;

// Cadastro e login da Etapa 5: o domínio guarda só o hash e o repositório é o único dono da lista.
public class AutenticacaoServico
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public AutenticacaoServico(IUsuarioRepositorio usuarios)
    {
        _usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
    }

    public Usuario Cadastrar(
        string nome,
        string email,
        string telefone,
        string endereco,
        TipoPerfil tipoPerfil,
        string senha,
        decimal? valorHora = null,
        TipoCobranca? tipoCobranca = null)
    {
        if (string.IsNullOrWhiteSpace(senha))
        {
            throw new ArgumentException("Nome, e-mail e senha são obrigatórios.");
        }

        // REG-010: e-mail identifica o usuário, então a verificação ignora maiúsculas/minúsculas.
        if (_usuarios.ObterPorEmail(email) is not null)
        {
            throw new InvalidOperationException("E-mail já cadastrado");
        }

        // PasswordHasher ignora a instância recebida: o hash depende só da senha.
        var senhaHash = _hasher.HashPassword(null!, senha);
        var usuario = new Usuario(nome, email, telefone, endereco, tipoPerfil, senhaHash);

        // Configura a cobrança antes de salvar: se DefinirCobranca falhar, nada fica no repositório.
        if (valorHora is not null)
        {
            usuario.DefinirCobranca(valorHora.Value, tipoCobranca ?? TipoCobranca.POR_HORA);
        }

        return _usuarios.Adicionar(usuario);
    }

    public Usuario? Autenticar(string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
        {
            return null;
        }

        var usuario = _usuarios.ObterPorEmail(email);

        if (usuario is null)
        {
            return null;
        }

        var resultado = _hasher.VerifyHashedPassword(null!, usuario.SenhaHash, senha);

        if (resultado == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // REG-011: só quem provou saber a senha descobre que a conta está bloqueada.
        if (usuario.Status == StatusUsuario.BLOQUEADO)
        {
            throw new InvalidOperationException("Usuário está bloqueado");
        }

        return usuario;
    }
}
