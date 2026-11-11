using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

// Persistência real pela API: cada requisição abre o seu próprio escopo e DbContext, então
// qualquer dado lido numa segunda requisição veio do SQLite, não de estado em memória.
public class PersistenciaApiTests : IClassFixture<PetWalkApiFactory>
{
    private readonly PetWalkApiFactory _fabrica;
    private readonly HttpClient _client;

    public PersistenciaApiTests(PetWalkApiFactory fabrica)
    {
        _fabrica = fabrica;
        _client = fabrica.CreateClient();
    }

    [Fact]
    public async Task Cadastro_PersisteEntreRequisicoesIndependentes()
    {
        var usuarioId = await Cadastrar("Nina", "nina@petwalk.com", TipoPerfil.PASSEADOR, valorHora: 30m);

        // Requisição nova: o login só encontra a senha se o cadastro tiver sido gravado.
        var login = await _client.PostAsJsonAsync("/login", new { email = "nina@petwalk.com", senha = "123456" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(usuarioId, await LerInteiro(login, "id"));

        // Escopo de serviços independente do usado pelo request, para ler direto do banco.
        using var escopo = _fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();
        var persistido = contexto.Usuarios.SingleOrDefault(u => u.Id == usuarioId);

        Assert.NotNull(persistido);
        Assert.Equal("nina@petwalk.com", persistido!.Email);
        Assert.Equal(30m, persistido.ValorHora);
    }

    [Fact]
    public async Task PetCriado_PodeSerConsultadoEmRequisicaoIndependente()
    {
        var tutorId = await Cadastrar("Otto", "otto@petwalk.com", TipoPerfil.TUTOR);
        var petId = await CriarPet(tutorId, "Bolt");

        var obtido = await _client.GetAsync($"/usuarios/{tutorId}/pets/{petId}");
        Assert.Equal(HttpStatusCode.OK, obtido.StatusCode);
        Assert.Contains("Bolt", await obtido.Content.ReadAsStringAsync());

        var lista = await _client.GetFromJsonAsync<List<JsonElement>>($"/usuarios/{tutorId}/pets");
        var pet = Assert.Single(lista!);
        Assert.Equal(petId, pet.GetProperty("id").GetInt32());

        using var escopo = _fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

        Assert.NotNull(contexto.Pets.SingleOrDefault(p => p.Id == petId));
    }

    [Fact]
    public async Task ExcluirPet_SemPasseio_Devolve204ERemoveDoBanco()
    {
        var tutorId = await Cadastrar("Paula", "paula@petwalk.com", TipoPerfil.TUTOR);
        var petId = await CriarPet(tutorId, "Mel");

        var exclusao = await _client.DeleteAsync($"/usuarios/{tutorId}/pets/{petId}");

        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);

        var consulta = await _client.GetAsync($"/usuarios/{tutorId}/pets/{petId}");
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);

        using var escopo = _fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

        Assert.Null(contexto.Pets.SingleOrDefault(p => p.Id == petId));
    }

    [Fact]
    public async Task ExcluirPet_ComPasseio_Devolve409EMantemOPetPersistido()
    {
        var tutorId = await Cadastrar("Rafa", "rafa@petwalk.com", TipoPerfil.TUTOR);
        var passeadorId = await Cadastrar("Sofia", "sofia@petwalk.com", TipoPerfil.PASSEADOR, valorHora: 25m);
        var petId = await CriarPet(tutorId, "Thor");
        await SolicitarPasseio(tutorId, petId, passeadorId);

        var exclusao = await _client.DeleteAsync($"/usuarios/{tutorId}/pets/{petId}");

        Assert.Equal(HttpStatusCode.Conflict, exclusao.StatusCode);
        Assert.Contains("Pet possui passeios", await exclusao.Content.ReadAsStringAsync());

        // O pet continua acessível e continua na tabela.
        var consulta = await _client.GetAsync($"/usuarios/{tutorId}/pets/{petId}");
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);

        using var escopo = _fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

        Assert.NotNull(contexto.Pets.SingleOrDefault(p => p.Id == petId));
        Assert.NotNull(contexto.Passeios.SingleOrDefault(p => p.Pet.Id == petId));
    }

    [Fact]
    public async Task ExcluirPet_Inexistente_Devolve404()
    {
        var tutorId = await Cadastrar("Vera", "vera@petwalk.com", TipoPerfil.TUTOR);

        var exclusao = await _client.DeleteAsync($"/usuarios/{tutorId}/pets/9999");

        Assert.Equal(HttpStatusCode.NotFound, exclusao.StatusCode);
        Assert.Contains("Pet não encontrado", await exclusao.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CadastroComEmailRepetido_Devolve409()
    {
        await Cadastrar("Zelia", "zelia@petwalk.com", TipoPerfil.TUTOR);

        var repetido = await _client.PostAsJsonAsync("/cadastro", new
        {
            nome = "Zelia de novo",
            email = "zelia@petwalk.com",
            telefone = "11999999999",
            endereco = "Rua A, 10",
            tipoPerfil = (int)TipoPerfil.TUTOR,
            senha = "123456"
        });

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Contains("E-mail já cadastrado", await repetido.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MediaAvaliacao_ConsideraAvaliacoesDePasseiosDiferentes()
    {
        var passeadorId = await Cadastrar("Tania", "tania@petwalk.com", TipoPerfil.PASSEADOR, valorHora: 20m);

        var (primeiroTutorId, primeiroPasseioId) =
            await PasseioFinalizado("Ubirajara", "ubirajara@petwalk.com", passeadorId);
        var (segundoTutorId, segundoPasseioId) =
            await PasseioFinalizado("Vitor", "vitor@petwalk.com", passeadorId);

        var primeira = await Avaliar(primeiroPasseioId, primeiroTutorId, nota: 5);
        Assert.Equal(5m, await LerDecimal(primeira, "mediaAvaliacao"));

        // Segundo passeio, requisição nova: a média tem de considerar também a nota 5.
        var segunda = await Avaliar(segundoPasseioId, segundoTutorId, nota: 3);
        Assert.Equal(4m, await LerDecimal(segunda, "mediaAvaliacao"));

        // Persistido: a listagem pública devolve a média acumulada.
        var passeadores = await _client.GetFromJsonAsync<List<JsonElement>>("/passeadores");
        var tania = Assert.Single(passeadores!, p => p.GetProperty("id").GetInt32() == passeadorId);
        Assert.Equal(4m, tania.GetProperty("mediaAvaliacao").GetDecimal());
    }

    private async Task<(int tutorId, int passeioId)> PasseioFinalizado(string nome, string email, int passeadorId)
    {
        var tutorId = await Cadastrar(nome, email, TipoPerfil.TUTOR);
        var petId = await CriarPet(tutorId, "Bolt");
        var passeioId = await SolicitarPasseio(tutorId, petId, passeadorId);

        await Acao($"passeios/{passeioId}/aceitar", passeadorId);
        await Acao($"passeios/{passeioId}/iniciar", passeadorId);
        await Acao($"passeios/{passeioId}/finalizar", passeadorId);

        return (tutorId, passeioId);
    }

    private async Task Acao(string rota, int usuarioId)
    {
        var resposta = await _client.PostAsJsonAsync($"/{rota}", new { usuarioId });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    private async Task<HttpResponseMessage> Avaliar(int passeioId, int avaliadorId, int nota)
    {
        var resposta = await _client.PostAsJsonAsync($"/passeios/{passeioId}/avaliacoes",
            new { usuarioId = avaliadorId, nota, comentario = "Muito bom" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        return resposta;
    }

    private async Task<int> Cadastrar(string nome, string email, TipoPerfil tipoPerfil, decimal? valorHora = null)
    {
        var resposta = await _client.PostAsJsonAsync("/cadastro", new
        {
            nome,
            email,
            telefone = "11999999999",
            endereco = "Rua A, 10",
            tipoPerfil = (int)tipoPerfil,
            senha = "123456",
            valorHora,
            tipoCobranca = valorHora is null ? (int?)null : (int)TipoCobranca.POR_HORA
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        return await LerInteiro(resposta, "id");
    }

    private async Task<int> CriarPet(int tutorId, string nome)
    {
        var resposta = await _client.PostAsJsonAsync($"/usuarios/{tutorId}/pets", new
        {
            nome,
            especie = "Cachorro",
            raca = "Vira-lata",
            porte = (int)Porte.MEDIO,
            idade = 3,
            observacoes = "Muito dócil"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        return await LerInteiro(resposta, "id");
    }

    private async Task<int> SolicitarPasseio(int tutorId, int petId, int passeadorId)
    {
        var resposta = await _client.PostAsJsonAsync("/passeios", new
        {
            usuarioId = tutorId,
            petId,
            passeadorId,
            dataHora = Fabrica.Agora.AddDays(1),
            duracaoMinutos = 60,
            local = "Rua das Flores, 10"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        return await LerInteiro(resposta, "id");
    }

    private static async Task<int> LerInteiro(HttpResponseMessage resposta, string propriedade)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return documento.RootElement.GetProperty(propriedade).GetInt32();
    }

    private static async Task<decimal> LerDecimal(HttpResponseMessage resposta, string propriedade)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return documento.RootElement.GetProperty(propriedade).GetDecimal();
    }
}
