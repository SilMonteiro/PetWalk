using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

// Critérios (a)–(d) do D03: uma falha inesperada na gravação deve virar 500 com o corpo
// genérico, sem vazar o detalhe interno, com a exceção registrada em log do projeto e sem
// nada persistido. O critério (e) — 422 para regra violada e 409 para e-mail duplicado —
// já é coberto pelos testes existentes e não é duplicado aqui.
//
// A API é a real (PetWalkApiFactory, SQLite em memória). Só a IUnidadeDeTrabalho é
// substituída, por um fake que lança uma exceção simples: não é DbUpdateException, então o
// ErroDePersistenciaHandler não a reconhece e é o caminho do D03 que fica exposto.
public class FalhaInesperadaTests
{
    private const string MensagemGenerica = "Erro interno. Tente novamente mais tarde.";
    private const string SegredoInterno = "segredo-interno";

    // O handler ainda não existe nesta fatia: o nome é literal de propósito, para o teste
    // compilar e falhar por comportamento, não por símbolo ausente.
    private const string NomeDoHandler = "FalhaInesperadaHandler";

    // (a) A falha inesperada responde 500 com o corpo do contrato, lido pelo frontend.
    [Fact]
    public async Task FalhaInesperada_Devolve500ComMensagemGenerica()
    {
        using var fabrica = new FabricaComFalhaInesperada();
        var (tutorId, petId, passeadorId) = Semear(fabrica);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/passeios", Requisicao(tutorId, petId, passeadorId));
        var corpo = await resposta.Content.ReadAsStringAsync();

        // A falha tem de ser a da gravação: se o fake não foi chamado, o 500 veio de outro lugar.
        Assert.Equal(1, fabrica.Unidade.Chamadas);
        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);

        Assert.True(
            TentarLerErro(corpo, out var mensagem),
            $"corpo sem o campo 'erro': {corpo}");
        Assert.Equal(MensagemGenerica, mensagem);
    }

    // (b) O cliente não recebe a mensagem interna, o nome do tipo nem stack trace.
    [Fact]
    public async Task FalhaInesperada_NaoExpoeMensagemInternaNemStackTrace()
    {
        using var fabrica = new FabricaComFalhaInesperada();
        var (tutorId, petId, passeadorId) = Semear(fabrica);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/passeios", Requisicao(tutorId, petId, passeadorId));
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.DoesNotContain(SegredoInterno, corpo);
        Assert.DoesNotContain("StackTrace", corpo);
        Assert.DoesNotContain("System.Exception", corpo);
        Assert.DoesNotContain(NomeDoHandler, corpo);

        // Só faz sentido falar em "não vazar" se o corpo for o contrato de erro esperado;
        // sem esta linha, um corpo vazio também "não vazaria" nada.
        Assert.True(
            TentarLerErro(corpo, out var mensagem),
            $"corpo sem o campo 'erro': {corpo}");
        Assert.Equal(MensagemGenerica, mensagem);
    }

    // (c) O detalhe da exceção fica no log do projeto, com o método e o caminho HTTP.
    [Fact]
    public async Task FalhaInesperada_RegistraErroNoLogComMetodoECaminho()
    {
        using var fabrica = new FabricaComFalhaInesperada();
        var (tutorId, petId, passeadorId) = Semear(fabrica);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/passeios", Requisicao(tutorId, petId, passeadorId));
        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);

        var registro = Assert.Single(
            fabrica.Coletor.Entradas,
            entrada => entrada.Nivel == LogLevel.Error
                       && entrada.Excecao?.Message == SegredoInterno
                       && entrada.Categoria.EndsWith(NomeDoHandler, StringComparison.Ordinal));

        Assert.Contains("POST", registro.Mensagem);
        Assert.Contains("/passeios", registro.Mensagem);
    }

    // (d) A resposta 500 é a tratada e nada foi gravado: sem passeio e sem notificação.
    //
    // Nota: "nada persistido" já era verdadeiro antes do D03, porque a exceção acontece
    // antes do SaveChanges. A pré-condição do corpo tratado existe para o teste não passar
    // em silêncio quando a requisição falha em outro ponto.
    [Fact]
    public async Task FalhaInesperada_NaoPersistePasseioNemNotificacao()
    {
        using var fabrica = new FabricaComFalhaInesperada();
        var (tutorId, petId, passeadorId) = Semear(fabrica);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/passeios", Requisicao(tutorId, petId, passeadorId));
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(1, fabrica.Unidade.Chamadas);
        Assert.True(
            TentarLerErro(corpo, out var mensagem),
            $"corpo sem o campo 'erro': {corpo}");
        Assert.Equal(MensagemGenerica, mensagem);

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

        Assert.Empty(contexto.Passeios);
        Assert.Empty(contexto.Notificacoes);
    }

    private static SolicitacaoPasseioRequest Requisicao(int tutorId, int petId, int passeadorId) =>
        new(tutorId, petId, passeadorId, Fabrica.Agora.AddDays(1), 60, "Rua A, 10");

    // Mesmo padrão de EndpointsFalhaDePersistenciaTests: escopo novo sobre o banco da fábrica.
    private static (int TutorId, int PetId, int PasseadorId) Semear(PetWalkApiFactory fabrica)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

        var tutor = Fabrica.CriarUsuario(TipoPerfil.TUTOR);
        var pet = Fabrica.CriarPet(tutor);
        tutor.AdicionarPet(pet);

        var passeador = Fabrica.CriarUsuario(TipoPerfil.PASSEADOR);
        passeador.DefinirCobranca(30m, TipoCobranca.POR_HORA);

        contexto.Usuarios.Add(tutor);
        contexto.Usuarios.Add(passeador);
        contexto.Pets.Add(pet);
        contexto.SaveChanges();

        return (tutor.Id, pet.Id, passeador.Id);
    }

    private static bool TentarLerErro(string corpo, out string? mensagem)
    {
        mensagem = null;

        try
        {
            using var json = JsonDocument.Parse(corpo);

            if (json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("erro", out var erro))
            {
                mensagem = erro.GetString();
                return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed class FabricaComFalhaInesperada : PetWalkApiFactory
    {
        public UnidadeDeTrabalhoComExcecaoSimples Unidade { get; } = new();

        public ColetorDeLogs Coletor { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(servicos =>
            {
                servicos.RemoveAll<IUnidadeDeTrabalho>();
                servicos.AddSingleton<IUnidadeDeTrabalho>(Unidade);

                // Provider escrito no próprio teste: o projeto não depende de pacote de log
                // de teste, e o coletor só existe nesta fábrica.
                servicos.AddSingleton<ILoggerProvider>(Coletor);
            });
        }
    }

    private sealed class UnidadeDeTrabalhoComExcecaoSimples : IUnidadeDeTrabalho
    {
        public int Chamadas { get; private set; }

        public void Salvar()
        {
            Chamadas++;
            throw new Exception(SegredoInterno);
        }
    }

    private sealed class ColetorDeLogs : ILoggerProvider
    {
        private readonly List<EntradaDeLog> _entradas = new();
        private readonly object _trava = new();

        public IReadOnlyList<EntradaDeLog> Entradas
        {
            get
            {
                lock (_trava)
                {
                    return _entradas.ToArray();
                }
            }
        }

        public ILogger CreateLogger(string categoria) => new Coletor(categoria, this);

        public void Dispose()
        {
        }

        private void Registrar(EntradaDeLog entrada)
        {
            lock (_trava)
            {
                _entradas.Add(entrada);
            }
        }

        public sealed record EntradaDeLog(string Categoria, LogLevel Nivel, string Mensagem, Exception? Excecao);

        private sealed class Coletor : ILogger
        {
            private readonly string _categoria;
            private readonly ColetorDeLogs _dono;

            public Coletor(string categoria, ColetorDeLogs dono)
            {
                _categoria = categoria;
                _dono = dono;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                _dono.Registrar(new EntradaDeLog(_categoria, logLevel, formatter(state, exception), exception));
        }
    }
}
