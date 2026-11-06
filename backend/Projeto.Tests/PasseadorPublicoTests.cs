using System.Text.Json;

// REG-007: a listagem de passeadores (FLX-001) expõe só o perfil público.
public class PasseadorPublicoTests
{
    [Fact]
    public void Resposta_NaoExpoeDadosSensiveis()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);
        passeador.DefinirCobranca(40m, TipoCobranca.POR_HORA_COM_ADICIONAL_DE_PORTE);

        var resposta = PasseadorEndpoints.ParaResposta(passeador);

        // Mesmas opções que a API usa ao serializar (camelCase).
        var json = JsonSerializer.Serialize(resposta, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .ToLowerInvariant();

        Assert.DoesNotContain("endereco", json);
        Assert.DoesNotContain("email", json);
        Assert.DoesNotContain("telefone", json);
        Assert.DoesNotContain("senhahash", json);
        Assert.DoesNotContain("senha", json);
    }

    [Fact]
    public void Resposta_TemApenasOsCamposPublicos()
    {
        var nomes = typeof(PasseadorPublicoResponse)
            .GetProperties()
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal(new[] { "Id", "MediaAvaliacao", "Nome", "TipoCobranca", "ValorHora" }, nomes);
    }

    [Fact]
    public void Resposta_LeOsDadosDoPasseador()
    {
        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);
        passeador.DefinirId(7);
        passeador.DefinirCobranca(55.5m, TipoCobranca.POR_HORA);

        var resposta = PasseadorEndpoints.ParaResposta(passeador);

        Assert.Equal(7, resposta.Id);
        Assert.Equal(passeador.Nome, resposta.Nome);
        Assert.Equal(passeador.MediaAvaliacao, resposta.MediaAvaliacao);
        Assert.Equal(55.5m, resposta.ValorHora);
        Assert.Equal(TipoCobranca.POR_HORA, resposta.TipoCobranca);
    }
}
