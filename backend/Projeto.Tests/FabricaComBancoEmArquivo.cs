using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Host da API sobre um banco SQLite em arquivo. O schema não é criado aqui: quem usa precisa
// aplicar as migrations antes, porque o startup da API valida o histórico de migrations.
public sealed class FabricaComBancoEmArquivo : WebApplicationFactory<Program>
{
    private readonly string _caminho;

    public FabricaComBancoEmArquivo(string caminho) => _caminho = caminho;

    // Pooling=False: a conexão libera o arquivo ao ser descartada, permitindo apagá-lo no finally.
    private static string StringDeConexao(string caminho) => $"Data Source={caminho};Pooling=False";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(servicos =>
        {
            servicos.RemoveAll<DbContextOptions<PetWalkDbContext>>();
            servicos.RemoveAll<IDbContextOptionsConfiguration<PetWalkDbContext>>();

            servicos.AddDbContext<PetWalkDbContext>(options =>
                options.UseSqlite(StringDeConexao(_caminho)));
        });
    }
}
