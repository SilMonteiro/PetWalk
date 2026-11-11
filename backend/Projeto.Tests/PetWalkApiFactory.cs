using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

// Fábrica da API para testes de integração. Substitui o SQLite em arquivo da aplicação por
// um banco em memória cuja conexão vive enquanto a fábrica viver: cada instância da fábrica
// abre a sua própria conexão, então nenhuma instância enxerga os dados de outra.
public class PetWalkApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexao;

    public PetWalkApiFactory()
    {
        // No SQLite, ":memory:" existe apenas enquanto a conexão estiver aberta: ela é a
        // dona do banco. Por isso a conexão é criada aqui e descartada no Dispose.
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(servicos =>
        {
            // Remove o registro de produção (arquivo App_Data/petwalk.db) — tanto as opções
            // resolvidas quanto a configuração que as produz — antes de registrar o contexto
            // sobre a conexão da fábrica.
            servicos.RemoveAll<DbContextOptions<PetWalkDbContext>>();
            servicos.RemoveAll<IDbContextOptionsConfiguration<PetWalkDbContext>>();

            servicos.AddDbContext<PetWalkDbContext>(options => options.UseSqlite(_conexao));
        });
    }

    // O schema é criado pela fábrica, nunca pelo startup da aplicação: PetWalkDbContext
    // continua sem EnsureCreated e sem Migrate no Program.cs.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var escopo = host.Services.CreateScope();
        escopo.ServiceProvider.GetRequiredService<PetWalkDbContext>().Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        // O host é descartado primeiro: quando a conexão fecha, nenhum contexto ainda a usa.
        base.Dispose(disposing);

        if (disposing)
        {
            _conexao.Dispose();
        }
    }
}
