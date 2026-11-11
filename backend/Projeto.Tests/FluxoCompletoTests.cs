using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

// Fluxo ponta a ponta pela API real (PetWalkApiFactory): FLX-006 → FLX-007 → FLX-001 →
// FLX-002 → FLX-003 → FLX-008 → FLX-004 → FLX-005, conferindo as notificações geradas.
public class FluxoCompletoTests : IClassFixture<PetWalkApiFactory>
{
    private readonly HttpClient _client;

    public FluxoCompletoTests(PetWalkApiFactory fabrica)
    {
        _client = fabrica.CreateClient();
    }

    [Fact]
    public async Task FluxoCompleto_DoCadastroAteAvaliacaoENotificacao()
    {
        // FLX-006: cadastro do tutor e da passeadora, e login
        var tutorId = await Cadastrar("Maria", "maria@petwalk.com", tipoPerfil: 0);
        var passeadorId = await Cadastrar("Bia", "bia@petwalk.com", tipoPerfil: 1, valorHora: 30m);
        Assert.True(tutorId > 0);
        Assert.True(passeadorId > 0);

        var login = await _client.PostAsJsonAsync("/login", new { email = "maria@petwalk.com", senha = "123456" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(tutorId, await LerInteiro(login, "id"));

        // FLX-007: pet do tutor
        var pet = await _client.PostAsJsonAsync($"/usuarios/{tutorId}/pets", new
        {
            nome = "Rex",
            especie = "Cachorro",
            raca = "Vira-lata",
            porte = 2,
            idade = 3,
            observacoes = "Muito dócil"
        });
        Assert.Equal(HttpStatusCode.Created, pet.StatusCode);
        var petId = await LerInteiro(pet, "id");

        // FLX-001: a passeadora aparece na listagem pública
        var passeadores = await _client.GetFromJsonAsync<List<JsonElement>>("/passeadores");
        Assert.Contains(passeadores!, p => p.GetProperty("id").GetInt32() == passeadorId);

        // FLX-002: solicitação (1h × 30 × 1,3 do porte GRANDE)
        var solicitacao = await _client.PostAsJsonAsync("/passeios", new
        {
            usuarioId = tutorId,
            petId,
            passeadorId,
            dataHora = Fabrica.Agora.AddDays(1),
            duracaoMinutos = 60,
            local = "Rua das Flores, 10"
        });
        Assert.Equal(HttpStatusCode.Created, solicitacao.StatusCode);
        var passeioId = await LerInteiro(solicitacao, "id");
        Assert.Equal(39m, await LerDecimal(solicitacao, "valor"));
        Assert.Equal("Rua das Flores, 10", await LerTexto(solicitacao, "local"));

        // A passeadora recebeu PASSEIO_SOLICITADO (tipo 0)
        var pendentes = await Notificacoes(passeadorId, apenasNaoLidas: true);
        var solicitacaoNotificada = Assert.Single(pendentes);
        Assert.Equal(0, solicitacaoNotificada.GetProperty("tipo").GetInt32());
        Assert.Equal(passeioId, solicitacaoNotificada.GetProperty("passeioId").GetInt32());

        // FLX-003 / FLX-008 / FLX-004: aceite, início e fim
        await Acao($"passeios/{passeioId}/aceitar", passeadorId, HttpStatusCode.OK, statusEsperado: 1);
        await Acao($"passeios/{passeioId}/iniciar", passeadorId, HttpStatusCode.OK, statusEsperado: 2);
        await Acao($"passeios/{passeioId}/finalizar", passeadorId, HttpStatusCode.OK, statusEsperado: 3);

        // FLX-005: o tutor avalia e a média da passeadora é atualizada
        var avaliacao = await _client.PostAsJsonAsync($"/passeios/{passeioId}/avaliacoes", new
        {
            usuarioId = tutorId,
            nota = 5,
            comentario = "Muito atenciosa com o Rex"
        });
        Assert.Equal(HttpStatusCode.Created, avaliacao.StatusCode);
        Assert.Equal(5, await LerInteiro(avaliacao, "nota"));
        Assert.Equal(5m, await LerDecimal(avaliacao, "mediaAvaliacao"));
        Assert.Equal(passeadorId, await LerInteiro(avaliacao, "avaliado", "id"));

        // O tutor recebeu ACEITO, INICIADO e FINALIZADO; marcar uma como lida reduz as pendentes
        var doTutor = await Notificacoes(tutorId, apenasNaoLidas: true);
        Assert.Equal(3, doTutor.Count);
        Assert.Equal(new[] { 1, 3, 4 }, doTutor.Select(n => n.GetProperty("tipo").GetInt32()).ToArray());

        var marcar = await _client.PostAsJsonAsync(
            $"/notificacoes/{doTutor[0].GetProperty("id").GetInt32()}/lida", new { usuarioId = tutorId });
        Assert.Equal(HttpStatusCode.NoContent, marcar.StatusCode);
        Assert.Equal(2, (await Notificacoes(tutorId, apenasNaoLidas: true)).Count);
        Assert.Equal(3, (await Notificacoes(tutorId, apenasNaoLidas: false)).Count);
    }

    [Fact]
    public async Task AvaliarDuasVezes_Devolve422()
    {
        var tutorId = await Cadastrar("Carla", "carla@petwalk.com", tipoPerfil: 0);
        var passeadorId = await Cadastrar("Davi", "davi@petwalk.com", tipoPerfil: 1, valorHora: 20m);

        var pet = await _client.PostAsJsonAsync($"/usuarios/{tutorId}/pets", new
        {
            nome = "Mel", especie = "Gato", raca = "Siamês", porte = 0, idade = 2, observacoes = ""
        });
        var petId = await LerInteiro(pet, "id");

        var solicitacao = await _client.PostAsJsonAsync("/passeios", new
        {
            usuarioId = tutorId,
            petId,
            passeadorId,
            dataHora = Fabrica.Agora.AddDays(2),
            duracaoMinutos = 60,
            local = "Rua A, 10"
        });
        var passeioId = await LerInteiro(solicitacao, "id");

        await Acao($"passeios/{passeioId}/aceitar", passeadorId, HttpStatusCode.OK, statusEsperado: 1);
        await Acao($"passeios/{passeioId}/iniciar", passeadorId, HttpStatusCode.OK, statusEsperado: 2);
        await Acao($"passeios/{passeioId}/finalizar", passeadorId, HttpStatusCode.OK, statusEsperado: 3);

        var primeira = await _client.PostAsJsonAsync($"/passeios/{passeioId}/avaliacoes", new { usuarioId = tutorId, nota = 5, comentario = "" });
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);

        var segunda = await _client.PostAsJsonAsync($"/passeios/{passeioId}/avaliacoes", new { usuarioId = tutorId, nota = 3, comentario = "" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, segunda.StatusCode);
        Assert.Contains("Passeio já avaliado por este usuário", await segunda.Content.ReadAsStringAsync());
    }

    private async Task<int> Cadastrar(string nome, string email, int tipoPerfil, decimal? valorHora = null)
    {
        var resposta = await _client.PostAsJsonAsync("/cadastro", new
        {
            nome,
            email,
            telefone = "11999999999",
            endereco = "Rua A, 10",
            tipoPerfil,
            senha = "123456",
            valorHora,
            // POR_HORA_COM_ADICIONAL_DE_PORTE: o pet GRANDE da 1,3 × valor por hora.
            tipoCobranca = valorHora is null ? (int?)null : 1
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        return await LerInteiro(resposta, "id");
    }

    private async Task Acao(string rota, int usuarioId, HttpStatusCode esperado, int statusEsperado)
    {
        var resposta = await _client.PostAsJsonAsync($"/{rota}", new { usuarioId });

        Assert.Equal(esperado, resposta.StatusCode);
        Assert.Equal(statusEsperado, await LerInteiro(resposta, "status"));
    }

    private async Task<List<JsonElement>> Notificacoes(int usuarioId, bool apenasNaoLidas)
    {
        var url = $"/usuarios/{usuarioId}/notificacoes?naoLidas={apenasNaoLidas.ToString().ToLowerInvariant()}";
        var resposta = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var notificacoes = await resposta.Content.ReadFromJsonAsync<List<JsonElement>>();

        return notificacoes!;
    }

    private static async Task<int> LerInteiro(HttpResponseMessage resposta, params string[] caminho)
    {
        var valor = await Ler(resposta, caminho);

        return valor.GetInt32();
    }

    private static async Task<decimal> LerDecimal(HttpResponseMessage resposta, params string[] caminho)
    {
        var valor = await Ler(resposta, caminho);

        return valor.GetDecimal();
    }

    private static async Task<string> LerTexto(HttpResponseMessage resposta, params string[] caminho)
    {
        var valor = await Ler(resposta, caminho);

        return valor.GetString()!;
    }

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta, params string[] caminho)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var atual = documento.RootElement;

        foreach (var parte in caminho)
        {
            atual = atual.GetProperty(parte);
        }

        // Clone porque o documento é descartado ao sair do using.
        return atual.Clone();
    }
}
