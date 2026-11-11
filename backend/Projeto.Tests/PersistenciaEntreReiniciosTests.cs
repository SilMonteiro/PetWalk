using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

// 8.5: o banco em arquivo sobrevive ao descarte do host. As duas instâncias são hosts distintos
// sobre o mesmo arquivo — nada é compartilhado em memória entre elas.
public class PersistenciaEntreReiniciosTests
{
    private static string CaminhoTemporario() =>
        Path.Combine(Path.GetTempPath(), $"petwalk-reinicio-{Guid.NewGuid():N}.db");

    // Migrations, nunca EnsureCreated: o startup da API rejeita tabelas sem histórico de migrations.
    private static void AplicarMigrations(string caminho)
    {
        var opcoes = new DbContextOptionsBuilder<PetWalkDbContext>()
            .UseSqlite($"Data Source={caminho};Pooling=False")
            .Options;

        using var contexto = new PetWalkDbContext(opcoes);
        contexto.Database.Migrate();
    }

    [Fact]
    public async Task DadosDaPrimeiraInstancia_ContinuamDisponiveisNaSegunda()
    {
        var caminho = CaminhoTemporario();

        try
        {
            AplicarMigrations(caminho);

            int tutorId;
            int petId;

            // Instância A: o bloco fecha antes de B existir, então o host e a conexão já foram
            // descartados quando a segunda instância começa.
            using (var primeira = new FabricaComBancoEmArquivo(caminho))
            {
                using var cliente = primeira.CreateClient();

                var cadastro = await cliente.PostAsJsonAsync("/cadastro", new
                {
                    nome = "Reinicio",
                    email = "reinicio@petwalk.com",
                    telefone = "11999999999",
                    endereco = "Rua A, 10",
                    tipoPerfil = (int)TipoPerfil.TUTOR,
                    senha = "123456"
                });

                Assert.Equal(HttpStatusCode.OK, cadastro.StatusCode);

                using (var documento = JsonDocument.Parse(await cadastro.Content.ReadAsStringAsync()))
                {
                    tutorId = documento.RootElement.GetProperty("id").GetInt32();
                }

                var pet = await cliente.PostAsJsonAsync($"/usuarios/{tutorId}/pets", new
                {
                    nome = "Rex",
                    especie = "Cachorro",
                    raca = "Vira-lata",
                    porte = (int)Porte.MEDIO,
                    idade = 3,
                    observacoes = "Muito dócil"
                });

                Assert.Equal(HttpStatusCode.Created, pet.StatusCode);

                using (var documento = JsonDocument.Parse(await pet.Content.ReadAsStringAsync()))
                {
                    petId = documento.RootElement.GetProperty("id").GetInt32();
                }
            }

            // Instância B: host novo sobre o mesmo arquivo. O login revalida a senha contra o
            // hash gravado por A, e a listagem lê a linha do pet que A criou.
            using var segunda = new FabricaComBancoEmArquivo(caminho);
            using var clienteDaSegunda = segunda.CreateClient();

            var login = await clienteDaSegunda.PostAsJsonAsync("/login",
                new { email = "reinicio@petwalk.com", senha = "123456" });

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            using (var documento = JsonDocument.Parse(await login.Content.ReadAsStringAsync()))
            {
                Assert.Equal(tutorId, documento.RootElement.GetProperty("id").GetInt32());
            }

            var consulta = await clienteDaSegunda.GetAsync($"/usuarios/{tutorId}/pets");

            Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);

            using var documentoDosPets = JsonDocument.Parse(await consulta.Content.ReadAsStringAsync());
            var petPersistido = Assert.Single(documentoDosPets.RootElement.EnumerateArray());

            Assert.Equal(petId, petPersistido.GetProperty("id").GetInt32());
            Assert.Equal("Rex", petPersistido.GetProperty("nome").GetString());
        }
        finally
        {
            File.Delete(caminho);
        }
    }
}
