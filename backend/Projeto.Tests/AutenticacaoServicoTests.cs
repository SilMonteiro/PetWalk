// Cadastro feliz do AutenticacaoServico; a cobertura completa do serviço entra no Prompt 7.
public class AutenticacaoServicoTests
{
    [Fact]
    public void Cadastrar_GravaUsuarioComIdEHashDaSenha()
    {
        var servico = new AutenticacaoServico(new UsuarioRepositorioEmMemoria());

        var usuario = servico.Cadastrar(
            "Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");

        Assert.Equal(1, usuario.Id);
        Assert.Equal("ana@petwalk.com", usuario.Email);
        Assert.NotEqual("senha123", usuario.SenhaHash);
    }
}
