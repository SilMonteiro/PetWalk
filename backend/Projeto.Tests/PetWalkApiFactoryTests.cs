using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// A fábrica troca o SQLite em arquivo por um banco em memória, isolado por instância, e
// mantém o schema disponível para os testes durante toda a vida da fábrica.
public class PetWalkApiFactoryTests
{
    [Fact]
    public void Fabrica_RegistraOHandlerDePersistencia()
    {
        using var fabrica = new PetWalkApiFactory();

        // O wiring do pipeline depende deste registro: sem ele, DbUpdateException vira 500.
        Assert.Contains(fabrica.Services.GetServices<IExceptionHandler>(),
            handler => handler is ErroDePersistenciaHandler);
    }

    [Fact]
    public void Fabrica_SubstituiOBancoDeArquivoPorSqliteEmMemoria()
    {
        using var fabrica = new PetWalkApiFactory();
        using var escopo = fabrica.Services.CreateScope();

        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();
        var conexao = contexto.Database.GetDbConnection();

        // A conexão é a da fábrica, aberta sobre ":memory:" — ela mantém o banco vivo.
        Assert.IsType<SqliteConnection>(conexao);
        Assert.Contains(":memory:", conexao.ConnectionString);
        Assert.Equal(ConnectionState.Open, conexao.State);
    }

    [Fact]
    public void Fabrica_DisponibilizaContextoComSchemaCriado()
    {
        using var fabrica = new PetWalkApiFactory();
        using var escopo = fabrica.Services.CreateScope();

        var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

        // Sem o schema criado, a consulta lançaria "no such table: Usuarios".
        Assert.Empty(contexto.Usuarios);
        Assert.True(contexto.Database.CanConnect());
    }

    [Fact]
    public void FabricasDistintas_NaoCompartilhamDadosNoBanco()
    {
        using var primeira = new PetWalkApiFactory();
        using var segunda = new PetWalkApiFactory();

        using (var escopo = primeira.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

            contexto.Add(Fabrica.CriarUsuario(TipoPerfil.TUTOR, "isolado@petwalk.com"));
            contexto.SaveChanges();
        }

        using (var escopo = segunda.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>();

            Assert.Empty(contexto.Usuarios);
        }
    }

    [Fact]
    public async Task FabricasDistintas_NaoCompartilhamDadosDaApi()
    {
        using var primeira = new PetWalkApiFactory();
        using var segunda = new PetWalkApiFactory();

        using var clientePrimeira = primeira.CreateClient();

        var cadastro = await clientePrimeira.PostAsJsonAsync("/cadastro", new
        {
            nome = "Ana",
            email = "ana@petwalk.com",
            telefone = "11999999999",
            endereco = "Rua A, 10",
            tipoPerfil = 1,
            senha = "123456",
            valorHora = 25m,
            tipoCobranca = 0
        });
        Assert.Equal(HttpStatusCode.OK, cadastro.StatusCode);

        var daPrimeira = await clientePrimeira.GetFromJsonAsync<List<JsonElement>>("/passeadores");
        Assert.Single(daPrimeira!);

        using var clienteSegunda = segunda.CreateClient();

        var daSegunda = await clienteSegunda.GetFromJsonAsync<List<JsonElement>>("/passeadores");
        Assert.Empty(daSegunda!);
    }
}
