// Cadastro e login do AutenticacaoServico (REG-010 e REG-011), com o repositório em memória real.
public class AutenticacaoServicoTests
{
    private static (AutenticacaoServico servico, UsuarioRepositorioEmMemoria repositorio) CriarServico()
    {
        var repositorio = new UsuarioRepositorioEmMemoria();

        return (new AutenticacaoServico(repositorio), repositorio);
    }

    [Fact]
    public void Cadastrar_GravaUsuarioComIdEHashDaSenha()
    {
        var (servico, _) = CriarServico();

        var usuario = servico.Cadastrar(
            "Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");

        Assert.Equal(1, usuario.Id);
        Assert.Equal("ana@petwalk.com", usuario.Email);
    }

    // REG-010
    [Fact]
    public void Cadastrar_ComEmailJaCadastrado_LancaErro()
    {
        var (servico, _) = CriarServico();
        servico.Cadastrar("Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");

        var erro = Assert.Throws<InvalidOperationException>(
            () => servico.Cadastrar("Outra", "ana@petwalk.com", "11888888888", "Rua B, 20", TipoPerfil.PASSEADOR, "outraSenha"));

        Assert.Equal("E-mail já cadastrado", erro.Message);
    }

    // REG-010
    [Fact]
    public void Cadastrar_ComEmailEmCaixaDiferente_LancaErro()
    {
        var (servico, _) = CriarServico();
        servico.Cadastrar("Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");

        var erro = Assert.Throws<InvalidOperationException>(
            () => servico.Cadastrar("Outra", "ANA@PetWalk.com", "11888888888", "Rua B, 20", TipoPerfil.PASSEADOR, "outraSenha"));

        Assert.Equal("E-mail já cadastrado", erro.Message);
    }

    [Fact]
    public void Cadastrar_GuardaApenasOHashDaSenha()
    {
        var (servico, _) = CriarServico();
        const string senha = "senha123";

        var usuario = servico.Cadastrar(
            "Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, senha);

        Assert.NotEqual(senha, usuario.SenhaHash);
        Assert.DoesNotContain(senha, usuario.SenhaHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cadastrar_ComSenhaEmBranco_LancaErro(string senha)
    {
        var (servico, _) = CriarServico();

        var erro = Assert.Throws<ArgumentException>(
            () => servico.Cadastrar("Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, senha));

        Assert.Equal("Nome, e-mail e senha são obrigatórios.", erro.Message);
    }

    [Fact]
    public void Autenticar_ComCredenciaisCorretas_DevolveUsuario()
    {
        var (servico, _) = CriarServico();
        var cadastrado = servico.Cadastrar("Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");

        var autenticado = servico.Autenticar("ana@petwalk.com", "senha123");

        Assert.Same(cadastrado, autenticado);
    }

    [Fact]
    public void Autenticar_ComSenhaErrada_DevolveNulo()
    {
        var (servico, _) = CriarServico();
        servico.Cadastrar("Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");

        Assert.Null(servico.Autenticar("ana@petwalk.com", "senha-errada"));
    }

    [Fact]
    public void Autenticar_ComEmailInexistente_DevolveNulo()
    {
        var (servico, _) = CriarServico();
        servico.Cadastrar("Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");

        Assert.Null(servico.Autenticar("ninguem@petwalk.com", "senha123"));
    }

    // REG-011
    [Fact]
    public void Autenticar_UsuarioBloqueadoComSenhaCerta_LancaErro()
    {
        var (servico, _) = CriarServico();
        var usuario = servico.Cadastrar("Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");
        usuario.Bloquear();

        var erro = Assert.Throws<InvalidOperationException>(() => servico.Autenticar("ana@petwalk.com", "senha123"));

        Assert.Equal("Usuário está bloqueado", erro.Message);
    }

    // REG-011: quem erra a senha não descobre que a conta está bloqueada.
    [Fact]
    public void Autenticar_UsuarioBloqueadoComSenhaErrada_DevolveNulo()
    {
        var (servico, _) = CriarServico();
        var usuario = servico.Cadastrar("Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");
        usuario.Bloquear();

        Assert.Null(servico.Autenticar("ana@petwalk.com", "senha-errada"));
    }

    [Fact]
    public void Cadastrar_PasseadorComValorHoraETipo_GravaACobranca()
    {
        var (servico, _) = CriarServico();

        var usuario = servico.Cadastrar(
            "Bia", "bia@petwalk.com", "11888888888", "Rua B, 20", TipoPerfil.PASSEADOR, "senha123",
            45.5m, TipoCobranca.POR_HORA_COM_ADICIONAL_DE_PORTE);

        Assert.Equal(45.5m, usuario.ValorHora);
        Assert.Equal(TipoCobranca.POR_HORA_COM_ADICIONAL_DE_PORTE, usuario.TipoCobranca);
    }

    [Fact]
    public void Cadastrar_PasseadorSoComValorHora_UsaPorHora()
    {
        var (servico, _) = CriarServico();

        var usuario = servico.Cadastrar(
            "Bia", "bia@petwalk.com", "11888888888", "Rua B, 20", TipoPerfil.PASSEADOR, "senha123", 30m);

        Assert.Equal(30m, usuario.ValorHora);
        Assert.Equal(TipoCobranca.POR_HORA, usuario.TipoCobranca);
    }

    [Fact]
    public void Cadastrar_SoComTipoSemValorHora_IgnoraOTipo()
    {
        var (servico, _) = CriarServico();

        var usuario = servico.Cadastrar(
            "Bia", "bia@petwalk.com", "11888888888", "Rua B, 20", TipoPerfil.PASSEADOR, "senha123",
            tipoCobranca: TipoCobranca.POR_HORA_COM_ADICIONAL_DE_PORTE);

        Assert.Equal(0m, usuario.ValorHora);
        Assert.Equal(TipoCobranca.POR_HORA, usuario.TipoCobranca);
    }

    [Fact]
    public void Cadastrar_SemCamposDeCobranca_MantemOPadrao()
    {
        var (servico, repositorio) = CriarServico();

        var usuario = servico.Cadastrar(
            "Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123");

        Assert.Equal(0m, usuario.ValorHora);
        Assert.Equal(TipoCobranca.POR_HORA, usuario.TipoCobranca);
        Assert.Same(usuario, repositorio.ObterPorEmail("ana@petwalk.com"));
    }

    [Fact]
    public void Cadastrar_TutorComValorHora_LancaErroENaoSalva()
    {
        var (servico, repositorio) = CriarServico();

        var erro = Assert.Throws<InvalidOperationException>(
            () => servico.Cadastrar(
                "Ana", "ana@petwalk.com", "11999999999", "Rua A, 10", TipoPerfil.TUTOR, "senha123", 30m));

        Assert.Equal("Usuário não possui perfil de passeador", erro.Message);
        Assert.Null(repositorio.ObterPorEmail("ana@petwalk.com"));
    }

    [Fact]
    public void Cadastrar_ComValorHoraNegativo_LancaErroENaoSalva()
    {
        var (servico, repositorio) = CriarServico();

        var erro = Assert.Throws<ArgumentException>(
            () => servico.Cadastrar(
                "Bia", "bia@petwalk.com", "11888888888", "Rua B, 20", TipoPerfil.PASSEADOR, "senha123", -10m));

        Assert.Equal("Valor por hora deve ser maior que zero.", erro.Message);
        Assert.Null(repositorio.ObterPorEmail("bia@petwalk.com"));
    }
}
