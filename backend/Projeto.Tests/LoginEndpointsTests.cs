using System.Text.Json;
using Microsoft.AspNetCore.Http;

// FLX-006: o login devolve o perfil e o endereço do próprio usuário (REG-007), sem dados
// sensíveis, mantendo 401 para credenciais inválidas e 403 para usuário bloqueado (REG-011).
public class LoginEndpointsTests
{
    private static AutenticacaoServico CriarServico() =>
        new(new UsuarioRepositorioEmMemoria());

    private static Usuario Cadastrar(AutenticacaoServico autenticacao, TipoPerfil tipoPerfil,
        string endereco = "Rua das Flores, 10") =>
        autenticacao.Cadastrar("Maria", "maria@petwalk.com", "11999999999", endereco, tipoPerfil, "123456");

    [Fact]
    public void Login_Tutor_DevolveTipoPerfil0Eendereco()
    {
        var autenticacao = CriarServico();
        Cadastrar(autenticacao, TipoPerfil.TUTOR);

        var resultado = UsuarioEndpoints.Login(new LoginRequest("maria@petwalk.com", "123456"), autenticacao);

        using var corpo = CorpoDe(resultado);
        Assert.Equal(200, StatusDe(resultado));
        Assert.Equal(0, corpo.RootElement.GetProperty("tipoPerfil").GetInt32());
        Assert.Equal("Rua das Flores, 10", corpo.RootElement.GetProperty("endereco").GetString());
        Assert.Equal("Maria", corpo.RootElement.GetProperty("nome").GetString());
        Assert.Equal("maria@petwalk.com", corpo.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public void Login_Passeador_DevolveTipoPerfil1()
    {
        var autenticacao = CriarServico();
        Cadastrar(autenticacao, TipoPerfil.PASSEADOR, "Rua B, 20");

        var resultado = UsuarioEndpoints.Login(new LoginRequest("maria@petwalk.com", "123456"), autenticacao);

        using var corpo = CorpoDe(resultado);
        Assert.Equal(1, corpo.RootElement.GetProperty("tipoPerfil").GetInt32());
        Assert.Equal("Rua B, 20", corpo.RootElement.GetProperty("endereco").GetString());
    }

    [Fact]
    public void Login_Resposta_NaoExpoeSenhaHashStatusNemTelefone()
    {
        var autenticacao = CriarServico();
        Cadastrar(autenticacao, TipoPerfil.TUTOR);

        var resultado = UsuarioEndpoints.Login(new LoginRequest("maria@petwalk.com", "123456"), autenticacao);

        var texto = TextoDe(resultado);
        Assert.DoesNotContain("senha", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("telefone", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("status", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Login_CredenciaisInvalidas_Mantem401()
    {
        var autenticacao = CriarServico();
        Cadastrar(autenticacao, TipoPerfil.TUTOR);

        var resultado = UsuarioEndpoints.Login(new LoginRequest("maria@petwalk.com", "senha-errada"), autenticacao);

        Assert.Equal(401, StatusDe(resultado));
        Assert.Equal("E-mail ou senha inválidos", MensagemDe(resultado));
    }

    [Fact]
    public void Login_UsuarioBloqueado_Mantem403()
    {
        var autenticacao = CriarServico();
        var usuario = Cadastrar(autenticacao, TipoPerfil.TUTOR);
        usuario.Bloquear();

        var resultado = UsuarioEndpoints.Login(new LoginRequest("maria@petwalk.com", "123456"), autenticacao);

        Assert.Equal(403, StatusDe(resultado));
        Assert.Equal("Usuário está bloqueado", MensagemDe(resultado));
    }

    private static int StatusDe(IResult resultado) =>
        Assert.IsAssignableFrom<IStatusCodeHttpResult>(resultado).StatusCode!.Value;

    private static JsonDocument CorpoDe(IResult resultado) =>
        JsonDocument.Parse(TextoDe(resultado));

    private static string TextoDe(IResult resultado)
    {
        var valor = Assert.IsAssignableFrom<IValueHttpResult>(resultado).Value;

        return JsonSerializer.Serialize(valor);
    }

    private static string? MensagemDe(IResult resultado)
    {
        using var corpo = CorpoDe(resultado);

        return corpo.RootElement.GetProperty("erro").GetString();
    }
}
