using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// A verificação de schema roda no startup da API: interrompe antes de qualquer requisição quando
// o banco SQLite não tem as migrations aplicadas ou não corresponde ao modelo. Os testes usam
// banco em arquivo temporário, porque é onde migrations fazem sentido; o banco em memória da
// fábrica é isento.
public class VerificacaoDeSchemaTests
{
    private static string CaminhoTemporario() =>
        Path.Combine(Path.GetTempPath(), $"petwalk-schema-{Guid.NewGuid():N}.db");

    // Pooling=False: a conexão libera o arquivo ao ser descartada, permitindo apagá-lo no finally.
    private static string StringDeConexao(string caminho) => $"Data Source={caminho};Pooling=False";

    private static PetWalkDbContext CriarContexto(string caminho) =>
        new(new DbContextOptionsBuilder<PetWalkDbContext>()
            .UseSqlite(StringDeConexao(caminho))
            .Options);

    private static void AplicarMigrations(string caminho)
    {
        using var contexto = CriarContexto(caminho);
        contexto.Database.Migrate();
    }

    // Concatena a mensagem da exceção e das internas: o host pode embrulhar a falha de startup.
    private static string Mensagens(Exception erro)
    {
        var texto = new StringBuilder();

        for (var atual = erro; atual is not null; atual = atual.InnerException)
        {
            texto.AppendLine(atual.Message);
        }

        return texto.ToString();
    }

    [Fact]
    public void BancoInexistente_InterrompeComInstrucaoDeMigracao()
    {
        var caminho = CaminhoTemporario();

        try
        {
            using var contexto = CriarContexto(caminho);

            var erro = Assert.Throws<InvalidOperationException>(
                () => VerificacaoDeSchema.GarantirSchemaAplicado(contexto));

            Assert.Contains("não existe", erro.Message);
            Assert.Contains("dotnet ef database update", erro.Message);
            Assert.DoesNotContain(caminho, erro.Message);
            Assert.DoesNotContain("Data Source", erro.Message);

            // A verificação não cria o banco: quem cria o schema é a migration.
            Assert.False(File.Exists(caminho));
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void BancoVazio_InterrompeIndicandoMigrationsPendentes()
    {
        var caminho = CaminhoTemporario();
        File.WriteAllBytes(caminho, Array.Empty<byte>());

        try
        {
            using var contexto = CriarContexto(caminho);

            var erro = Assert.Throws<InvalidOperationException>(
                () => VerificacaoDeSchema.GarantirSchemaAplicado(contexto));

            Assert.Contains("migrations pendentes", erro.Message);
            Assert.Contains("_Inicial", erro.Message);
            Assert.Contains("dotnet ef database update", erro.Message);

            // Sem histórico aplicado, o problema é de migrations — não de schema divergente.
            Assert.DoesNotContain("não corresponde", erro.Message);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void HistoricoDeMigrationsVazio_InterrompeIndicandoDivergencia()
    {
        var caminho = CaminhoTemporario();

        try
        {
            AplicarMigrations(caminho);

            using (var dano = CriarContexto(caminho))
            {
                // A tabela de histórico existe, mas sem nenhum registro, e as tabelas do modelo
                // também existem: aplicar migrations falharia nas tabelas já criadas.
                dano.Database.ExecuteSqlRaw("DELETE FROM __EFMigrationsHistory");
            }

            using var contexto = CriarContexto(caminho);

            var erro = Assert.Throws<InvalidOperationException>(
                () => VerificacaoDeSchema.GarantirSchemaAplicado(contexto));

            Assert.Contains("não corresponde ao histórico", erro.Message);
            Assert.DoesNotContain("dotnet ef database update", erro.Message);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void BancoCriadoComEnsureCreated_InterrompeIndicandoDivergencia()
    {
        var caminho = CaminhoTemporario();

        try
        {
            using (var criacao = CriarContexto(caminho))
            {
                Assert.True(criacao.Database.EnsureCreated());
            }

            // EnsureCreated cria as tabelas, mas não registra a migration: o schema existe sem
            // histórico que o explique, então database update falharia nas tabelas existentes.
            using var contexto = CriarContexto(caminho);

            var erro = Assert.Throws<InvalidOperationException>(
                () => VerificacaoDeSchema.GarantirSchemaAplicado(contexto));

            Assert.Contains("tabelas do modelo", erro.Message);
            Assert.Contains("não corresponde ao histórico", erro.Message);
            Assert.DoesNotContain("dotnet ef database update", erro.Message);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void BancoCriadoComEnsureCreatedParcial_InterrompeIndicandoDivergencia()
    {
        var caminho = CaminhoTemporario();

        try
        {
            using (var criacao = CriarContexto(caminho))
            {
                criacao.Database.EnsureCreated();
            }

            using (var dano = CriarContexto(caminho))
            {
                dano.Database.ExecuteSqlRaw("DROP TABLE Notificacoes");
            }

            using var contexto = CriarContexto(caminho);

            var erro = Assert.Throws<InvalidOperationException>(
                () => VerificacaoDeSchema.GarantirSchemaAplicado(contexto));

            // Ainda existem tabelas do modelo: o caso é divergência, não banco vazio.
            Assert.Contains("tabelas do modelo", erro.Message);
            Assert.DoesNotContain("dotnet ef database update", erro.Message);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void BancoComMigrationsAplicadas_NaoInterrompe()
    {
        var caminho = CaminhoTemporario();

        try
        {
            using (var criacao = CriarContexto(caminho))
            {
                criacao.Database.Migrate();

                Assert.Contains("_Inicial", criacao.Database.GetAppliedMigrations());
            }

            // Cobre também a comparação de tabelas e colunas contra o modelo.
            using var contexto = CriarContexto(caminho);

            VerificacaoDeSchema.GarantirSchemaAplicado(contexto);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void TabelaAusenteAposMigrations_InterrompeIndicandoSchemaIncompleto()
    {
        var caminho = CaminhoTemporario();

        try
        {
            AplicarMigrations(caminho);

            using (var dano = CriarContexto(caminho))
            {
                dano.Database.ExecuteSqlRaw("DROP TABLE Notificacoes");
            }

            using var contexto = CriarContexto(caminho);

            var erro = Assert.Throws<InvalidOperationException>(
                () => VerificacaoDeSchema.GarantirSchemaAplicado(contexto));

            Assert.Contains("não corresponde", erro.Message);
            Assert.Contains("faltam tabelas", erro.Message);
            Assert.Contains("Notificacoes", erro.Message);

            // Histórico já aplicado: repetir a migration não corrige o banco.
            Assert.DoesNotContain("dotnet ef database update", erro.Message);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void ColunaAusenteAposMigrations_InterrompeIndicandoSchemaIncompleto()
    {
        var caminho = CaminhoTemporario();

        try
        {
            AplicarMigrations(caminho);

            using (var dano = CriarContexto(caminho))
            {
                dano.Database.ExecuteSqlRaw("ALTER TABLE Usuarios DROP COLUMN Nome");
            }

            using var contexto = CriarContexto(caminho);

            var erro = Assert.Throws<InvalidOperationException>(
                () => VerificacaoDeSchema.GarantirSchemaAplicado(contexto));

            Assert.Contains("não corresponde", erro.Message);
            Assert.Contains("faltam colunas", erro.Message);
            Assert.Contains("Usuarios.Nome", erro.Message);
            Assert.DoesNotContain("dotnet ef database update", erro.Message);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void CaminhoQueNaoEhArquivo_NaoEhTratadoComoInexistente()
    {
        var diretorio = Path.Combine(Path.GetTempPath(), $"petwalk-schema-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(diretorio);

            using var contexto = CriarContexto(diretorio);

            var erro = Assert.Throws<InvalidOperationException>(
                () => VerificacaoDeSchema.GarantirSchemaAplicado(contexto));

            Assert.Contains("não pôde ser acessado", erro.Message);
            Assert.DoesNotContain("não existe", erro.Message);
            Assert.DoesNotContain(diretorio, erro.Message);
        }
        finally
        {
            Directory.Delete(diretorio);
        }
    }

    [Fact]
    public void ArquivoInvalido_InterrompeSemExporDetalhesDoProvedor()
    {
        var caminho = CaminhoTemporario();
        File.WriteAllText(caminho, "isto não é um banco SQLite");

        try
        {
            using var contexto = CriarContexto(caminho);

            var erro = Assert.Throws<InvalidOperationException>(
                () => VerificacaoDeSchema.GarantirSchemaAplicado(contexto));

            Assert.Contains("Não foi possível ler o schema", erro.Message);
            Assert.DoesNotContain(caminho, erro.Message);
            Assert.DoesNotContain("SQLite Error", erro.Message);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void BancoEmMemoria_NaoEhValidado()
    {
        using var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();

        using var contexto = new PetWalkDbContext(
            new DbContextOptionsBuilder<PetWalkDbContext>().UseSqlite(conexao).Options);

        contexto.Database.EnsureCreated();

        // É exatamente o caminho da PetWalkApiFactory: DbConnection já aberta e nenhuma
        // connection string nas opções. Por isso a verificação lê a conexão, não as opções.
        Assert.Null(contexto.Database.GetConnectionString());

        VerificacaoDeSchema.GarantirSchemaAplicado(contexto);
    }

    [Fact]
    public void ApiSemSchema_InterrompeNaInicializacao()
    {
        var caminho = CaminhoTemporario();

        try
        {
            using var fabrica = new FabricaComBancoEmArquivo(caminho);

            var erro = Assert.ThrowsAny<Exception>(() => { _ = fabrica.Services; });

            var mensagens = Mensagens(erro);

            Assert.Contains("dotnet ef database update", mensagens);
            Assert.DoesNotContain(caminho, mensagens);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public async Task ApiComMigrationsAplicadas_IniciaEResponde()
    {
        var caminho = CaminhoTemporario();

        try
        {
            AplicarMigrations(caminho);

            using var fabrica = new FabricaComBancoEmArquivo(caminho);
            using var cliente = fabrica.CreateClient();

            var resposta = await cliente.GetAsync("/passeadores");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    // Fábrica que aponta para um banco em arquivo, sem aplicar migrations: reproduz o ambiente
    // de produção antes do dotnet ef database update.
    private sealed class FabricaComBancoEmArquivo : WebApplicationFactory<Program>
    {
        private readonly string _caminho;

        public FabricaComBancoEmArquivo(string caminho) => _caminho = caminho;

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
}
